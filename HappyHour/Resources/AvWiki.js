(function () {
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

    function _parseActor(xpath) {
        var actorArray = _parseMultiNode(xpath, function (n) { return n; });
        if (actorArray == null) {
            return null;
        }

        var array = [];
        for (var i = 0; i < actorArray.length; i++) {
            var node = actorArray[i];
            console.log('href : ' + node.href);
            var actor = {};
            var m = /av-actress\/([a-z-]+)/i.exec(node.href);
            if (m != null) {
                actor['name'] = m[1].replace('-', ' ');
                actor['name'] = actor['name'].replace(/^([a-z])| ([a-z])/gi,
                    function (m) { return m.toUpperCase(); });
            } else {
                actor['name'] = node.textContent.trim();
            }
            array.push(actor);
        }
        return array;
    }

    function _multiResult() {
        var urls = _parseMultiNode("//il[@class='search-readmore']/a/@href");
        if (urls == null) {
            return 'notfound';
        }
        if (url.length == 0) {
            CefSharp.PostMessage({ type: 'url', data: urls[0] });
            return 'redirected';
        } else {
            return 'ambiguous';
        }
    }

    if (_multiResult() != 'notfound') {
        return;
    }

    var items = {
        actor: {
            xpath: "//dl[@class='dltable']/dt[contains(., 'AV女優名')]/ollowing-sibling::dd/a",
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