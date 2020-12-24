(function() {
    var node = document.evaluate("{{xpath}}", document.body,
        null, XPathResult.FIRST_ORDERED_NODE_TYPE, null);
  return new XMLSerializer().serializeToString(node.singleNodeValue);
})();