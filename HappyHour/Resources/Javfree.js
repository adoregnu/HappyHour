(function () {
    const _PID = '{{pid}}';

    function _parseSingleNode(_xpath) {
        var result = document.evaluate(_xpath, document.body,
            null, XPathResult.FIRST_ORDERED_NODE_TYPE, null);
        var node = result.singleNodeValue;
        if (node != null) {
            return node.textContent.trim();
        }
        return null;
    }

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
    function _parseContent(xpath, msg) {
        var result = document.evaluate(xpath, document.body, null,
            XPathResult.FIRST_ORDERED_NODE_TYPE, null);
        const parser = [_date, _actor];

        var count = 0;
        var p = result.singleNodeValue;
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


    function _multiResult() {
        var result = document.evaluate(
            "//div[contains(@class,'content-loop')]//h2[@class='entry-title']/a",
            document.body, null, XPathResult.ORDERED_NODE_SNAPSHOT_TYPE, null);

        var re = new RegExp(_PID, 'i');
        for (var i = 0; i < result.snapshotLength; i++) {
            var node = result.snapshotItem(i);
            //if (node.href.match('/' + _PID + '/i')) {
            if (re.test(node.href)) {
                CefSharp.PostMessage({ type: 'url', data: node.href });
                return 'redirected';
            }
        }
        if (result.snapshotLength > 1) {
            return 'ambiguous';
        }
        return 'notfound';
    }

    if (_multiResult() != 'notfound') {
        return;
    }

    var items = {
        title: { xpath: "//h1[@class='entry-title']"},
        cover: { xpath: "//div[@class='entry-content']//img[1]/@src" },
        studio: { xpath: "//strong[contains(.,'Categorized in:')]/following-sibling::a[1]"},
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
            msg[key] = _parseSingleNode(item['xpath']);
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