(function () {
    var root = document.evaluate("{{xpath}}", document.body,
        null, XPathResult.ORDERED_NODE_ITERATOR_TYPE, null);

    var array = [];
    while (node = root.iterateNext()) {
        array.push(new XMLSerializer().serializeToString(node));
    }
  return array;
}) ();