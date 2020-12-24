(function () {
    var root = document.evaluate("{{xpath}}", document.body,
        null, XPathResult.ORDERED_NODE_ITERATOR_TYPE, null);

    while (node = root.iterateNext()) {
        node.click();
    }
})();