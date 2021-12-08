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

    function parseSearchResult() {
        var urls = _parseMultiNode("//a[@class='movie-box']/@href");
        if (urls == null) {
            CefSharp.PostMessage({ type: 'items', data: 0 });
        } else if (urls.length == 1) {
            CefSharp.PostMessage({ type: 'url', data: urls[0] });
        } else {
            console.log('ambiguous result!');
        }
    }

    if (document.location.href.includes('/search/')) {
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