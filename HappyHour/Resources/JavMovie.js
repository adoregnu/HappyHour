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

function _parseMultiNode(xpath) {
    var result = document.evaluate(xpath, document.body,
        null, XPathResult.ORDERED_NODE_ITERATOR_TYPE, null);

    var array = [];
    while (node = result.iterateNext()) {
        array.push(node.textContent.trim());
    }
    if (array.length > 0) {
        return array;
    }
    return null;
}

function _parseActor(xpath) {
    var result = document.evaluate(xpath, document.body,
        null, XPathResult.ORDERED_NODE_ITERATOR_TYPE, null);

    var array = [];
    while (node = result.iterateNext()) {
        var actor = {};
        var m = /([\w\s]+) \((.+)\)/i.exec(node.textContent);
        if (m == null) {
            actor['name'] = node.textContent.trim();
        } else {
            actor['name'] = m[1];
            var alias = [];
            while ((arr = /([\w\s]+),?/ig.exec(m[2])) !== null) {
                alias.push(arr[1]);
            }
            if (alias.length > 0) {
                actor['alias'] = alias;
            }
        }
        array.push(actor);
    }
    return array;
}

function _multiResult() {
    var result = document.evaluate("//div[@class='movie-thumb']/a",
        document.body, null, XPathResult.ORDERED_NODE_SNAPSHOT_TYPE, null);
    if (result.snapshotLength == 1) {
        CefSharp.PostMessage({ type: 'url', data: result.snapshotItem(0).href });
        return 'redirected';
    } else if (result.snapshotLength > 1) {
        return 'ambiguous';
    }
    return 'notfound';
}

(function () {
    if (_multiResult() != 'notfound') {
        return;
    }

    var items = {
        cover: { xpath: "//div[@class='movie-cover']/img/@src" },
        title: { xpath: "//div[@class='mdm-info']/h1/text()" },
        date: { xpath: "//div[@class='mdm-info']//tr[2]/td[2]/text()" },
        studio: { xpath: "//div[@class='mdm-info']//tr[5]/td[2]//text()" },
        actor: {
            xpath: "//td[@class='list-actress']/a/text()",
            handler: _parseActor
        },
        genre: {
            xpath: "//td[@class='list-genre']/a",
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