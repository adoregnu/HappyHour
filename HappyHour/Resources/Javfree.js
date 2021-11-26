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

function _multiResult() {
    var result = document.evaluate("//h2[@class='entry-title']/a",
        document.body, null, XPathResult.ORDERED_NODE_SNAPSHOT_TYPE, null);
    
    for (var i = 0; i < result.snapshotLength; i++) {
        var node = result.snapshotItem(i);
        if (node.href.match('/' + _PID + '/i')) {
            CefSharp.PostMessage({ type: 'url', data: node.href });
            return 'redirected';
        }
    }
    if (result.snapshotLength > 1) {
        return 'ambiguous';
    }
    return 'notfound';
}

(function () {
    if (_multiResult() != 'notfound') {
        return;
    }

    var items = {
        //title: { xpath: "//div[@class='section-title']/h3/text()" },
        cover: { xpath: "//div[@class='entry-content']//img[1]/@src" },
    };

    var msg = { type : 'items' }
    var num_item = 0;
    for (var key in items) {
        var item = items[key];
        if (item["handler"] == null)
            msg[key] = _parseSingleNode(item['xpath']);
        else
            msg[key] = item['handler'](item['xpath']);
        //console.log(key + ': ' + msg[key]);
        num_item += 1;
    }
    msg['data'] = num_item;
    CefSharp.PostMessage(msg);
}) ();