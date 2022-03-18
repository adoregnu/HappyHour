(function () {
    const _PID = '{{pid}}';
    const _USERID = '{{userid}}';
    const _PASSWORD = '{{password}}';

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
        if (array.length > 0) {
            return array;
        }
        return null;
    }

    function get_node(n) { return n; }

    function _checkLogin() {
        var node = _parseSingleNode("//form[@id='sidebar_login_form']", get_node);
        if (node != null && _USERID.length > 0 && _PASSWORD.length > 0) {
            document.querySelector("div.sidebar-login input[name='mb_id']").value = _USERID;
            document.querySelector("div.sidebar-login input[name='mb_password']").value = _PASSWORD;
            document.querySelector("div.sidebar-login button").click();
            CefSharp.PostMessage({ type: 'items', data: 0});
            return false;
        }
        return true;
    }

    function parseSearchResult() {
        const xpath = "//div[@class='media-body']/a[" +
            "contains(@href, 'table=jamak') or " +
            "contains(@href,'table=bigsub')]";
        var url = _parseSingleNode(xpath, get_node);
        if (url != null) {
            CefSharp.PostMessage({ type: 'url', data: url.href});
        }
    }

    function downloadSub() {
        var node = _parseMultiNode("//a[contains(@class, 'view_file_download')]/@href");
        var title = _parseSingleNode("//h1[@itemprop='headline']/@content");
        var m = /[a-z0-9\-_]+/i.exec(title);
        if (m != null) {
            CefSharp.PostMessage({ type: 'sub', data: 2, urls : node, pid: m[0]});
        }
    }

    if (!_checkLogin()) {
        return;
    }

    if (document.location.href.includes('/bbs/download.php')) {
        var elm = document.getElementById('gda_downstop');
        if (elm != null) { elm.click(); }

        return;
    }

    if (document.location.href.includes('/bbs/board.php')) {
        if (/(bigsub|jamakbos|jamakuser)&wr_id/i.test(document.location.href)) {
            downloadSub();
        }
        return;
    }

    if (document.location.href.includes('/bbs/search.php')) {
        parseSearchResult();
        return;
    }

    var items = { };

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
    CefSharp.PostMessage(msg);
}) ();