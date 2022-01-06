(function () {
    const _BOARD = '{{board}}';
    const _PAGE_COUNT = '{{pageCount}}';

    function _parseSingleNode(_xpath, _getter = null) {
        var result = document.evaluate(_xpath, document.body,
            null, XPathResult.FIRST_ORDERED_NODE_TYPE, null);
        var node = result.singleNodeValue;
        if (node != null) {
            if (_getter != null) {
                return _getter(node);
            } else {
                return node.textContent.trim();
            }
        }
        return null;
    }

    function _parseMultiNode(xpath, _getter = null) {
        var result = document.evaluate(xpath, document.body,
            null, XPathResult.ORDERED_NODE_ITERATOR_TYPE, null);

        var array = [];
        while (node = result.iterateNext()) {
            if (_getter != null) {
                array.push(_getter(node));
            } else {
                array.push(node.textContent.trim());
            }
        }
        return array.length > 0 ? array : null;
    }

    function _get_node(n) { return n; }

    function _parseFiles(xpath) {
        var anodes = _parseMultiNode(xpath, _get_node);
        var array = [];
        if (anodes != null) {
            anodes.forEach(function (n) {
                //console.log(n.textContent);
                array.push(function () { n.click(); });
                //array.push(n.href);
            });
        }
        return array.length > 0 ? array : null;
    }

    function _selectBoard() {
        const boards = {
            censored: "//div[@id='category_1']//a[contains(., '亚洲有码原创')]",
            uncensored: "//div[@id='category_1']//a[contains(., '亚洲无码原创')]"
        };
        var node = _parseSingleNode(boards[_BOARD], _get_node);
        if (node != null) {
            CefSharp.PostMessage({ type: 'url', data: node.href });
            return "redirected";
        }
        return 'notfound';
    }

    function _getItemList() {
        var alist = _parseMultiNode(
            "//tbody[contains(@id, 'normalthread_')]/tr/th[1]/a[@class='s xst']", _get_node);

        var array = [];
        var num_miss = 0;
        if (alist != null) {
            //console.log('alist len:' + alist.length);
            alist.forEach(function (anode) {
                var m = /[a-z0-9-_]+/i.exec(anode.textContent);
                if (m != null) {
                    array.push({ url: anode.href, pid: m[0] });
                } else {
                    num_miss += 1;
                }
            });
        }

        if (array.length > 0) {
            CefSharp.PostMessage({
                type: 'url_list', data: array, miss: num_miss,
                curr_url: document.location.href
            });
            return "redirected";
        }
        return "notfound";
    }

    function _getPid(xpath) {
        var title = _parseSingleNode(xpath);
        if (title != null) {
            var m = /[a-z0-9-_]+/i.exec(title);
            if (m != null) {
                return m[0];
            }
        }
        return null;
    }

    if (_selectBoard() != 'notfound') {
        return;
    }

    if (_getItemList() != 'notfound') {
        return;
    }

    var items = {
        pid: { xpath: "//span[@id='thread_subject']/text()", handler: _getPid },
        date: { xpath: "(//em[contains(@id, 'authorposton')]/span/@title)[1]" },
        files: { xpath: "//a[contains(., '.torrent')]", handler: _parseFiles },
        images: {
            xpath: "(//td[contains(@id, 'postmessage_')])[1]//img[contains(@id, 'aimg_')]/@file",
            handler: _parseMultiNode
        },
    };

    var msg = { type : 'items' }
    var num_item = 0;
    for (var key in items) {
        var item = items[key];
        if (item["handler"] == null) {
            msg[key] = _parseSingleNode(item['xpath']);
        } else {
            msg[key] = item['handler'](item['xpath']);
        }
        if (msg[key] == null) {
            continue;
        }
        //console.log(key + ': ' + msg[key]);
        num_item += 1;
    }
    msg['data'] = num_item;
    console.log(JSON.stringify(msg));
    CefSharp.PostMessage(msg);
}) ();