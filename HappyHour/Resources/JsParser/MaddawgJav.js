(function () {
    const _PID = '{{pid}}';
    var actor_parsed = false;

    function checkHeader(headers, txt) {
        for (var i = 0; i < headers.length; i++) {
            if (txt.includes(headers[i])) {
                return headers[i];
            }
        }
        return null;
    }

    function _date(txt, msg) {
        const headers = ['更新日：', '配信日:']
        var header = checkHeader(headers, txt);
        if (header == null) {
            return false;
        }
        var m = /([0-9.\-\/]+)/i.exec(txt);
        if (m == null) {
            return false;
        }
        msg['date'] = m[1];
        return true;
    }

    function _actor(txt, msg) {
        if (actor_parsed) {
            return;
        }
        const headers = ['名前：', '出演:']
        var header = checkHeader(headers, txt);
        if (header == null) {
            return false;
        }

        array = [];
        actor = {};

        actor['name'] = txt.substring(txt.indexOf(header) + header.length);
        array.push(actor);
        msg['actor'] = array;
        actor_parsed = true;
        return true;
    }
/*
    function _studio(txt, msg) {
        const kw = 'レーベル:'
        if (txt.includes(kw)) {
            msg['studio'] = txt.substring(txt.indexOf(kw) + kw.length);
            return true;
        }
        return false;
    }
*/

    function get_node(node) { return node; }

    function _parseActorTag(xpath, msg) {
        if (actor_parsed) {
            return 0;
        }
        var nodes = _jav_parse_multi_node(xpath, get_node);
        if (nodes == null) {
            console.log('no tags!');
            return 0;
        }
        if (nodes.length == 3) {
            actor_parsed = true;
            msg['actor'] = [{ name: nodes[0].textContent.trim() }]
            return 1;
        }
        return 0;
    }

    function _parseContent(xpath, msg) {
        const parser = [_date, _actor];

        var count = 0;
        var p = _jav_parse_single_node(xpath, get_node);
        if (p == null || p.childNodes == null) {
            return count;
        }
        console.log('content node len:' + p.childNodes.length);
        for (var i = 0; i < p.childNodes.length; i++) {
            var node = p.childNodes[i];
            //console.log('nodeType : ' + node.nodeType);
            if (node.nodeType != Node.TEXT_NODE) {
                continue;
            }
            for (var j = 0; j < parser.length; j++) {
                if (parser[j](node.textContent.trim(), msg)) {
                    count += 1;
                    break;
                }
            }
        }
        return count;
    }

    function parseSearchResult() {
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
    }

    if (document.location.href.includes('/?s=' + _PID)) {
        parseSearchResult();
        return;
    }

    var items = {
        title: { xpath: "//div[@class='entry']/p"},
        cover: { xpath: "//div[@class='entry']/figure/img/@src" },
        studio: { xpath: "//strong[contains(.,'Categorized in:')]/following-sibling::a[1]"},
        actor: {
            xpath: "//strong[contains(.,'Tags:')]/following-sibling::a",
            handler: _parseActorTag
        },
        content: {
            xpath: "//div[@class='entry-content']/p",
            handler: _parseContent
        },
    };

    var msg = { type : 'items' }
    var num_item = 0;
    for (var key in items) {
        var item = items[key];
        if (item["handler"] == null) {
            msg[key] = _jav_parse_single_node(item['xpath']);
            if (msg[key] == null) {
                continue;
            }
            num_item += 1;
        } else {
            num_item += item['handler'](item['xpath'], msg);
        }
        //console.log(key + ': ' + msg[key]);
    }
    msg['data'] = num_item;
    console.log(JSON.stringify(msg));
    CefSharp.PostMessage(msg);
}) ();