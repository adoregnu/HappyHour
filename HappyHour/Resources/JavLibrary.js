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

function _parseRating(xpath) {
    var rating = _parseSingleNode(xpath);
    if (rating != null && rating.length > 0) {
        //console.log('JavLibrary: rating:' + rating);
        var re = new RegExp('([0-9.]+)', 'i');
        var m = re.exec(rating);
        if (m != null) return m[1];
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

function _parseActorName(xpath) {
    var result = document.evaluate(xpath, document.body,
        null, XPathResult.ORDERED_NODE_ITERATOR_TYPE, null);

    var array = [];
    while (node = result.iterateNext()) {
        //console.log('node : ' + node.nodeName + ', class:' + node.className);
        var names = { };
        var alias = [];
        var children = node.childNodes;
        //console.log('num children :' + children.length);
        for (var i = 0; i < children.length; i++) {
            var nameNode = children[i];
            if (nameNode.nodeName != 'SPAN') continue;
            if (nameNode.className.includes('icn_')) continue;
            var name = nameNode.textContent.trim().split(' ').reverse().join(' ');
            if (names['name'] == null) {
                names['name'] = name;
            } else {
                alias.push(name);
            }
        }
        if (alias.length > 0) {
            names['alias'] = alias;
        }
        array.push(names);
        //console.log(JSON.stringify(names));
    }
    return array;
}

function _multiResult() {
    var result = document.evaluate("//div[@class='videos']/div/a",
        document.body, null, XPathResult.ORDERED_NODE_ITERATOR_TYPE, null);

    const re = new RegExp('^' + _PID, 'i');
    var num_item = 0;
    while (node = result.iterateNext()) {
        num_item += 1;
        //console.log(re + ', ' + node.title);
        if (re.test(node.title) !== null) {
            CefSharp.PostMessage({ type: 'url', data: node.href });
            return 'redirected';
        }
    }
    if (num_item > 0)
        return 'ambiguous';
    else
        return 'notfound';
}

(function () {
    if (_multiResult() != 'notfound') {
        return;
    }

    var items = {
        title: { xpath: "//*[@id='video_title']/h3/a" },
        date: { xpath: "//*[@id='video_date']//td[2]" },
        studio: { xpath: "//*[@id='video_maker']//*[@class='maker']/a" },
        cover: { xpath: "//*[@id='video_jacket_img']/@src" },
        rating: {
            xpath: "//*[@id='video_review']//*[@class='score']",
            handler: _parseRating
        },
        genre: {
            xpath: "//*[@id='video_genres']//*[@class='genre']//text()",
            handler: _parseMultiNode
        },
        actor: {
            xpath: "//*[@id='video_cast']//*[@class='cast']",
            handler: _parseActorName
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
        num_item += 1;
    }
    msg['data'] = num_item;
    console.log(JSON.stringify(msg));
    CefSharp.PostMessage(msg);
}) ();