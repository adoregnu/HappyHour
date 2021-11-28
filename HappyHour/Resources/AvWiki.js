const _PID = '{{pid}}';

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

function _parseActor(xpath) {

    var node = _parseSingleNode(xpath, function (n) { return n; });
    if (node == null) {
        return null;
    }
    const re = new RegExp('av-actress/([a-z-]+)', 'i');

    console.log('href : ' + node.href);
    var array = [];
    var actor = {};
    var m = re.exec(node.href);
    if (m != null) {
        actor['name'] = m[1].replace('-', ' ');
        actor['name'] = actor['name'].replace(/^([a-z])| ([a-z])/gi,
            function (m) { return m.toUpperCase(); });
    } else {
        actor['name'] = node.textContent.trim();
    }
    array.push(actor);
    return array;
}

(function () {

    var result = document.evaluate("//article[contains(@class,'archive-list')]",
        document.body, null, XPathResult.ORDERED_NODE_SNAPSHOT_TYPE, null);
    console.log('snapshotLength: ' + result.snapshotLength);
    if (result.snapshotLength != 1) {
        CefSharp.PostMessage({ type: 'items', data: 0 });
        return;
    }

    var items = {
        actor: { xpath: "//p[@class='actress-name']//a", handler: _parseActor },
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