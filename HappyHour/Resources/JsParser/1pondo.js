(function () {
    const _PID = '{{pid}}';

    function _parseSingleNode(_xpath, _getter = null, _node = document.body) {
        var result = document.evaluate(_xpath, _node,
            null, XPathResult.FIRST_ORDERED_NODE_TYPE, null);
        var node = result.singleNodeValue;
        if (node != null) {
            if (_getter != null) {
                return _getter(node);
            } else {
                var txt = node.textContent.trim();
                return txt.length < 2 ? null : txt;
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

    //function get_node(node) { return node; }

    function parseActor(xpath) {
        var names = _parseMultiNode(xpath);
        if (names == null) { return null; }
        var array = [];
        for (var i = 0; i < names.length; i++) {
            array.push({ name: names[i] });
        }
        if (array.length > 0) {
            return array;
        }
        return null;
    }

    var items = {
        //id: { xpath: "//th[contains(., '品番：')]/following-sibling::td" },
        title: { xpath: "//h1[@class='h1--dense']" },
        cover: { xpath: "//video[@class='vjs-tech']/@poster" },
        //studio: { xpath: "//th[contains(., 'メーカー：')]/following-sibling::td/a" },
        date: { xpath: "//span[contains(.,'Release Date:')]/following-sibling::span" },
        actor: {
            xpath: "//span[contains(.,'Featuring:')]/following-sibling::span/a",
            handler: parseActor
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