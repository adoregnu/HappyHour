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

    function get_node(node) { return node; }

    function parseList(xpath) {
        var nodes = _parseMultiNode(xpath, get_node);
        if (nodes == null) {
            return null;
        }
        var list = [];
        for (var i = 0; i < nodes.length; i++) {
            var item = {}
            var title = _parseSingleNode("td[2]/a[starts-with(.,'+++ [HD]')]", null, nodes[i]);
            if (title != null) {
                item['title'] = title
                item['torrent'] = _parseSingleNode("td[3]/a[1]", function (node) { return node.href }, nodes[i]);
                item['magnet'] = _parseSingleNode("td[3]/a[2]", function (node) { return node.href }, nodes[i]);

                list.push(item);
            }
        }

        return list;
    }
    function parseLink(xpath) {
        return _parseSingleNode(xpath, function (node) { return node.href; });
    }

    var items = {
        torrents: { xpath: "//tr[@class='success']", handler: parseList },
        nextPage: { xpath: "//li[@class='active']/following-sibling::li/a", handler: parseLink }
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