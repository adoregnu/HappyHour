(function () {
    const _PID = '{{pid}}';

    function check_header(headers, txt) {
        for (var i = 0; i < headers.length; i++) {
            if (txt.trim().startsWith(headers[i])) {
                return headers[i];
            }
        }
        return null;
    }

    function _date(txt, msg) {
        const headers = ['配信開始日', '更新日', '配信日', '発売日'];
        var header = check_header(headers, txt);
        if (header == null) {
            return false;
        }
        var m = /([0-9.\-\/]+)/i.exec(txt);
        if (m == null) {
            return false;
        }
        msg['date'] = m[1].trim();
        return true;
    }

    function _actor(txt, msg) {
        const headers = ['名前', '出演女優', '出演者', '出演' ];
        var header = check_header(headers, txt);
        if (header == null) {
            return false;
        }

        array = [];
        names = txt.substring(txt.indexOf(header) + header.length);
        names = names.replace(/^[：: —-]+/g, '');
        if (names.length < 2) {
            return false;
        }
        names = names.split(/[\/ ]+/)
        names.forEach(function (name) {
            actor = {};
            name = name.replace(/[()\d]+/, '');
            actor['name'] = name.trim();
            array.push(actor);
        });

        msg['actor'] = array;
        return true;
    }

    function _extrac_item(txt, msg, key, headers) {
        var header = check_header(headers, txt);

        if (header == null) {
            return false;
        }
        item = txt.substring(txt.indexOf(header) + header.length);
        item = item.replace(/^[：: —-]+/g, '').trim();
        if (item.length < 2) {
            return false;
        }
        msg[key] = item
        return true;
    }
    function _maker(txt, msg) {
        return _extrac_item(txt, msg, 'maker', ['メーカー']);
    }

    function _label(txt, msg) {
        return _extrac_item(txt, msg, 'label', ['レーベル']);
    }

    function _series(txt, msg) {
        return _extrac_item(txt, msg, 'series', ['シリーズ']);
    }
    function get_node(node) { return node; }

    function _parse_content(xpath, msg) {
        var parsers = [
            { func: _date, parsed: false },
            { func: _actor, parsed: false },
            { func: _maker, parsed: false },
            { func: _label, parsed: false },
            { func: _series, parsed: false }
        ];

        var count = 0;
        var p = _jav_parse_single_node(xpath, get_node);
        if (p == null || p.childNodes == null) {
            return count;
        }
        var lasNode = null;
        for (var i = 0; i < p.childNodes.length; i++) {
            var node = p.childNodes[i];
            if (node.nodeType != Node.TEXT_NODE) {
                continue;
            }
            for (var j = 0; j < parsers.length; j++) {
                parser = parsers[j]
                if (!parser.parsed) {
                    parser.parsed = parser.func(node.textContent.trim(), msg);
                    if  (parser.parsed) count += 1;
                }
            }
            lastNode = node;
        }
        var plot = lastNode.textContent.trim();
        if (plot.length > 10) {
            
            msg['plot'] = plot.replace(/※この作品.+/, '');
        }
        return count;
    }

    function get_node(node) { return node; }
    function _parse_images(xpath, msg) {
        var nodes = _jav_parse_multi_node(xpath, get_node)
        if (nodes == null) return 0;
        var screenshot = [];
        for (var i = 0; i < nodes.length; i++) {
            if (i == 0) {
                msg['cover'] = nodes[i].src;
            } else {
                screenshot.push(nodes[i].src);
            }
        }
        msg['screenshot'] = screenshot;
        return nodes.length;
    }

    function parseSearchResult() {
        var nodes = _jav_parse_multi_node("//div[contains(@class,'content-loop')]//h2[@class='entry-title']/a", get_node);
        if (nodes == null || nodes.length == 0) {
            console.log('no result!');
            CefSharp.PostMessage({ type: 'items', data: 0 });
            return;
        }

        for (var i = 0; i < nodes.length; i++) {
            var node = nodes[i];
            //check if node.href contains _PID case insensitive
            if (node.href.toLowerCase().includes(_PID.toLowerCase())) {
                CefSharp.PostMessage({ type: 'url', data: node.href });
                return;
            }
        }
        console.log('ambiguous result!');

        /*
        var result = document.evaluate(
            "//div[contains(@class,'content-loop')]//h2[@class='entry-title']/a",
            document.body, null, XPathResult.ORDERED_NODE_SNAPSHOT_TYPE, null);

        if (result.snapshotLength == 0) {
            CefSharp.PostMessage({ type: 'items', data: 0 });
        } else if (result.snapshotLength = 1) {
            var node = result.snapshotItem(0);

            CefSharp.PostMessage({ type: 'url', data: node.href });
        } else {
            console.log('ambiguous result!');
        }
        */
    }

    if (document.location.href.includes('/search/' + _PID)) {
        parseSearchResult();
        return;
    }

    var items = {
        title: { xpath: "//h1[@class='entry-title']"},
        images: {
            xpath: "//div[@class='entry-content']//img",
            handler: _parse_images
        },
        content: {
            xpath: "//div[@class='entry-content']//p",
            handler: _parse_content
        },
    };

    var msg = { type : 'items' }
    var num_item = 0;
    for (var key in items) {
        var item = items[key];
        if (item["handler"] == null) {
            msg[key] = _jav_parse_single_node(item['xpath']);
            if (msg[key] != null) {
                num_item += 1;
            }
        } else {
            num_item += item['handler'](item['xpath'], msg);
        }
        //console.log(key + ': ' + msg[key]);
    }
    msg['data'] = num_item;
    _post_message(msg, 'jp');
}) ();