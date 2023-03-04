(function () {
    const _PID = '{{pid}}';

    function _parseSingleNode(_xpath, _getter = null, _node = document.body, _result = null) {
        var result = document.evaluate(_xpath, _node,
            null, XPathResult.FIRST_ORDERED_NODE_TYPE, _result);
        var node = result.singleNodeValue;
        if (node != null) {
            if (_getter != null) {
                return _getter(node);
            } else {
                return node.textContent.trim();
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

    function _parseActor(xpath) {
        var array = _parseMultiNode(xpath, get_node);
        if (array == null) {
            return null;
        }
        var names = [];
        array.forEach(function (node) {
            var s = node.nextSibling;
            while (s != null && s.nodeName != 'STRONG') {
                s = s.nextSibling;
            }
            if (s != null && s.className == 'symbol female') {
                names.push({ name: node.textContent.trim(), link: node.href });
            }
        });
        if (names.length > 0) {
            return names;
        }
        return null;
    }

    function _parseRating(xpath) {
        var rating = _parseSingleNode(xpath);
        if (rating != null && rating.length > 0) {
            var m = /([0-9.]+),/i.exec(rating);
            if (m != null) return m[1];
        }
        return null;
    }

    function _parseStudio(xpath) {
        var text = _parseSingleNode(xpath);
        if (text != null) {
            return text.split(',')[0];
        }
        return null;
    }

    function _convertPid() {
        const re = new RegExp('^\d{6}(?:_|-)\d{3}$');
        if (_PID.startsWith('HEYZO') || re.test(_PID)) {
            return _PID.replace('_', '-');
        }
        return _PID;
    }

    function parseSearchResult() {
        var nodes = _parseMultiNode("//a[@class='box']", get_node);
        if (nodes == null) {
            CefSharp.PostMessage({ type: 'items', data: 0 });
            return;
        }

        var url;
        var num_match = 0;
        const re = new RegExp(_convertPid(), 'i');
        for (var i = 0; i < nodes.length; i++) {
            //var pid = _parseSingleNode("div[@class='uid']", null, nodes[i]);
            var pid = _parseSingleNode("div[@class='video-title']", null, nodes[i]);
            if (pid != null && re.test(pid)) {
                num_match += 1;
                url = nodes[i].href;
            }
        }

        if (num_match == 0) {
            CefSharp.PostMessage({ type: 'items', data: 0 });
        } else if (num_match == 1) {
            CefSharp.PostMessage({ type: 'url', data: url });
        } else {
            console.log('ambiguous result!');
        }
    }

    function _parseActorPage() {
        var txt = _parseSingleNode("//span[@class='actor-section-name']");
        if (txt == null) {
            return;
        }
        var actors = [];
        var tmp = txt.split(',');
        tmp = tmp.length > 1 ? tmp[1].trim() : tmp[0].trim();
        var actor = { name: tmp.replace(/\(uncensored\)/i, '')};

        xpath = "//div[contains(@class, 'actor-avatar')]//span[@class='avatar']/@style";
        txt = _parseSingleNode(xpath);
        if (txt != null && (m = /url\((.+)\)/i.exec(txt)) != null) {
            actor["thumb"] = m[1];
        }
        actors.push(actor);

        var msg = { type: 'items', data: 1, actor: actors };
        console.log(JSON.stringify(msg));
        CefSharp.PostMessage(msg);
    }

    if (document.location.href.includes('/actors/')) {
        _parseActorPage();
        return;
    }

    if (document.location.href.includes('/search?q=')) {
        parseSearchResult();
        return;
    }

    var items = {
        title: { xpath: "//strong[@class='current-title']/text()" },
        //cover: { xpath: "//div[@class='column column-video-cover']/a/@href" },
        cover: { xpath: "//img[@class='video-cover']/@src" },
        date: { xpath: "//strong[contains(., 'Released Date')]/following-sibling::span/text()" },
        studio: {
            xpath: "//strong[contains(., 'Maker')]/following-sibling::span/a/text()",
            handler: _parseStudio
        },
        series: { xpath: "//strong[contains(., 'Series')]/following-sibling::span/a/text()" },
        rating: {
            xpath: "//span[@class='score-stars']/following-sibling::text()",
            handler: _parseRating
        },
        actor: {
            xpath: "//strong[contains(., 'Actor')]/following-sibling::span/a",
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
        //if (key == 'cover2') key = 'cover';
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