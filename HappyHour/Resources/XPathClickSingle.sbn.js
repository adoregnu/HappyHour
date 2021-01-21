(function () {
    var root = document.evaluate("{{xpath}}", document.body,
        null, XPathResult.FIRST_ORDERED_NODE_TYPE, null);

    var node = root.singleNodeValue;
    node.click();
})();