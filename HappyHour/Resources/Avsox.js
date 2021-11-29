const _PID = '{{pid}}';

function _parseSingleNode(xpath, _node = document.body, _result = null) {
    var result = document.evaluate(xpath, _node,
        null, XPathResult.FIRST_ORDERED_NODE_TYPE, _result);
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
    while (anode = result.iterateNext()) {
        var actor = {};

        if (!anode.src.endsWith('nowprinting.gif')) {
            actor["thumb"] = anode.src;
        }
        var tnode = anode.parentNode.nextSibling;
        while (tnode.nodeName != "SPAN") {
            console.log('node name ' + tnode.nodeName);
            tnode = tnode.nextSibling;
        }
        actor["name"] = tnode.textContent.trim();
        array.push(actor);
    }

    if (array.length > 0) {
        return array;
    }
    return null;
}

function _convertPid() {
    const re = new RegExp('^\d{6}(?:_|-)\d{3}$');
    if (_PID.startsWith('HEYZO') || re.test(_PID)) {
        return _PID.replace('_', '-');
    }
    return _PID;
}

function _multiResult() {
    var result = document.evaluate("//a[contains(@class,'movie-box')]",
        document.body, null, XPathResult.ORDERED_NODE_SNAPSHOT_TYPE, null);

    const re = new RegExp(_convertPid(), 'i');
    for (var i = 0; i < result.snapshotLength; i++) {
        var anode = result.snapshotItem(i);
        var pid = _parseSingleNode("//date[1]", anode, result);
        console.log('PID: ' + pid);
        if (pid != null || re.test(pid.replace('_', '-'))) {
            CefSharp.PostMessage({ type: 'url', data: anode.href });
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
    if (elm = document.querySelector('body > div.container-fluid > div.alert.alert-danger > h4')) {
        console.log(elm.textContent);
        CefSharp.PostMessage({ type:'items', data:0 });
        return;
    }

    var items = {
        cover: { xpath: "//a[@class='bigImage']/img/@src" },
        date: { xpath: "//span[contains(., 'Release Date')]/following-sibling::text()" },
        studio: { xpath: "//p[contains(., 'Studio')]/following-sibling::p//text()" },
        series: { xpath: "//p[contains(., 'Series')]/following-sibling::p//text()" },
        genre: {
            xpath: "//span[@class='genre']//text()",
            handler: _parseMultiNode
        },
        title: { xpath: "//div[@class='container']/h3/text()" },
        actor: {
            xpath: "//a[@class='avatar-box']//img",
            handler: _parseActor
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