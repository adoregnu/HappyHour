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
        return array;
    }

    function get_node(node) { return node; }

    function parseSearchResult() {
        var nodes = _parseMultiNode("//h3[@class='post-title entry-title']/a", get_node);
        for (var i = 0; i < nodes.length; i++) {
            //console.log(nodes[i].textContent)
            if (!/{{pid}}/i.test(nodes[i].textContent)) continue;
            CefSharp.PostMessage({ type: 'url', data: nodes[i].href });
            return;
        }
        CefSharp.PostMessage({ type: 'items', data: 0 });
        return;
    }

    function parseCover(xpath) {
        var nodes = _parseMultiNode(xpath, get_node);
        if (nodes.length < 1) { return null; }

        for (var i = 0; i < nodes.length; i++) {
            if (nodes[i].naturalWidth > 900 || nodes[i].naturalHeight > 900) {
                continue;
            }
            return nodes[i].src;
        }
    }
    alert = function () { }

    if (document.location.href.includes('search?q=')) {
        parseSearchResult();
        return;
    }

    var items = {
        date: { xpath: "//td[contains(.,'商品発売日')]/following-sibling::td" },
        cover: {
            xpath: "//div[@class='post-body entry-content']/a/img",
            handler: parseCover
        }
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