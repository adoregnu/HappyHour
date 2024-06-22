function _jav_parse_single_node(_xpath, _getter = null, _node = document.body) {
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
function _jav_parse_multi_node(xpath, _getter = null) {
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
