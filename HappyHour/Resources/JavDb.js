﻿const _PID = '{{pid}}';

function _parseSingleNode(_xpath) {
    var result = document.evaluate(_xpath, document.body,
        null, XPathResult.FIRST_ORDERED_NODE_TYPE, null);
    var node = result.singleNodeValue;
    if (node != null) {
        return node.textContent.trim();
    }
    return null;
}

function _parseMultiNode(xpath) {
    var result = document.evaluate(xpath, document.body,
        null, XPathResult.ORDERED_NODE_ITERATOR_TYPE, null);

    var array = [];
    while (node = result.iterateNext()) {
        array.push(node.textContent.trim());
    }
    return array;
}

function _parseActor(xpath) {
    var result = document.evaluate(xpath, document.body,
        null, XPathResult.ORDERED_NODE_ITERATOR_TYPE, null);

    var array = [];
    while (node = result.iterateNext()) {
        var names = {};
        names['name'] = node.textContent.trim();
        array.push(names);
    }

    return array;
}

function _parseRating(xpath) {
    var rating = _parseSingleNode(xpath);
    if (rating != null && rating.length > 0) {
        var re = new RegExp('([0-9.]+),', 'i');
        var m = re.exec(rating);
        if (m != null) return m[1];
    }
    return null;
}

function _multiResult() {
    var result = document.evaluate("//a[@class='box']",
        document.body, null, XPathResult.ORDERED_NODE_ITERATOR_TYPE, null);
    var num_result = 0;
    while (node = result.iterateNext()) {
        var pidResult = document.evaluate("//div[@class='uid']", node,
            null, XPathResult.FIRST_ORDERED_NODE_TYPE, result);
        var pid = pidResult.singleNodeValue.textContent.trim();
        if (pid.localeCompare(_PID, undefined, { sensitivity: 'accent' }) == 0) {
            CefSharp.PostMessage({ type: 'url', data: node.href });
            return 'redirected';
        }
        num_result += 1;
    }
    if (num_result > 0) 
        return 'ambiguous';
    return 'notfound';
}

(function () {
    if (_multiResult() != 'notfound') {
        return;
    }

    var items = {
        title: { xpath: "//h2[contains(@class, 'title')]/strong/text()" },
        cover: { xpath: "//div[@class='column column-video-cover']/a/@href" },
        cover2: { xpath: "//div[@class='column column-video-cover']/a/img/@src" },
        date: { xpath: "//strong[contains(., 'Released Date')]/following-sibling::span/text()" },
        studio: { xpath: "//strong[contains(., 'Maker')]/following-sibling::span/a/text()" },
        rating: {
            xpath: "//span[@class='score-stars']/following-sibling::text()",
            handler: _parseRating
        },
        actor: {
            xpath: "//strong[contains(., 'Actor')]/following-sibling::span/a/text()",
            handler: _parseActor
        },
        genre: {
            xpath: "//strong[contains(., 'Tags')]/following-sibling::span/a/text()",
            handler: _parseMultiNode
        },
    };

    var msg = { type : 'items' }
    var num_item = 0;
    for (var key in items) {
        var item = items[key];
        if (key == 'cover2') key = 'cover';
        if (msg[key] != null) continue;

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