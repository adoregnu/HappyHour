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

    function parseSearchResult() {
        var result = document.evaluate("//p[@class='tmb']/a",
            document.body, null, XPathResult.ORDERED_NODE_SNAPSHOT_TYPE, null);

        const re = new RegExp(_PID.replace('-', ''), 'i');
        for (var i = 0; i < result.snapshotLength; i++) {
            var node = result.snapshotItem(i);
            if (re.test(node.href)) {
                CefSharp.PostMessage({ type: 'url', data: node.href});
                return 'redirected';
            }
        }
        if (result.snapshotLength > 1) {
            console.log('ambiguous result');
        } else {
            CefSharp.PostMessage({ type: 'items', data: 0 });
        }
    }

    if (document.location.href.includes('/search/=')) {
        parseSearchResult();
        return;
    }

    var items = {
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