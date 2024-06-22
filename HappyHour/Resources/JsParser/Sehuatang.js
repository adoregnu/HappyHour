(function () {
    const _BOARD = '{{board}}';
    const _PAGE_COUNT = '{{pageCount}}';

    var msg = { type : 'items' }
    function _get_node(n) { return n; }

    function _parseFiles(xpath) {
        var anodes = _jav_parse_multi_node(xpath, _get_node);
        var array = [];
        if (anodes != null) {
            anodes.forEach(function (n) {
                console.log(n.textContent);
                array.push(function () { n.click(); });
                //array.push(n.href);
            });
        }
        return array.length > 0 ? array : null;
    }

    function _parseImages(xpath) {
        var nodes = _jav_parse_multi_node(xpath);
        var array = [];
        if (nodes == null) {
            return null;
        }
        var i = 0;
        nodes.forEach(function (url) {
            var ext = url.split('.').pop();
            var name = "unknown.jpg";
            if (i == 0) {
                name = msg['pid'] + "_cover." + ext;
            } else {
                name = msg['pid'] + "_screenshot" + i + "." + ext;
            }
            console.log(name, url);
            var imgInfo = {
                url: url,
                target: name,
                func: function () {
                    const link = document.createElement('a');
                    document.body.appendChild(link);
                    link.download = name;
                    link.href = url;
                    link.target = '_blank';
                    link.click();
                    document.body.removeChild(link);
                }
            };
            array.push(imgInfo);
            i++;
        });
        return array;
    }

    function _selectBoard() {
        const boards = {
            censored: "//div[@id='category_1']//a[contains(., '亚洲有码原创')]",
            uncensored: "//div[@id='category_1']//a[contains(., '亚洲无码原创')]"
        };
        var node = _jav_parse_single_node(boards[_BOARD], _get_node);
        if (node != null) {
            CefSharp.PostMessage({ type: 'url', data: node.href });
            return "redirected";
        }
        return 'notfound';
    }

    function _getItemList() {
        var alist = _jav_parse_multi_node(
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
        var title = _jav_parse_single_node(xpath);
        if (title != null) {
            var m = /[a-z0-9-_]+/i.exec(title);
            if (m != null) {
                return m[0];
            }
        }
        return null;
    }

    function parseDate(xpath) {
        var em = _jav_parse_single_node(xpath, _get_node);
        if (em == null) { return null; }
        var date = _jav_parse_single_node("span/@title", null, em);
        if (date != null) {
            return date;
        }
        console.log('em: ' + em.textContent);
        var m = /[0-9]+/.exec(em.textContent);
        if (m != null) {
            return em.textContent.substring(m.index);
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
        date: { xpath: "//em[contains(@id, 'authorposton')]", handler: parseDate },
        //files: { xpath: "//a[contains(., '.torrent')]", handler: _parseFiles },
        magnet: { xpath: "//div[@class='blockcode']/div[contains(@id, 'code_')]//li", handler: _jav_parse_multi_node }, 
        images: {
            xpath: "(//td[contains(@id, 'postmessage_')])[1]//img[contains(@id, 'aimg_')]/@file",
            handler: _parseImages
        },
    };

    var num_item = 0;
    for (var key in items) {
        var item = items[key];
        if (item["handler"] == null) {
            msg[key] = _jav_parse_single_node(item['xpath']);
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