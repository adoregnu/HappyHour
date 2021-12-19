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
        if (array.length > 0) {
            return array;
        }
        return null;
    }

    function get_node(node) { return node; }

    function _parseActor(xpath) {
        var nodes = _parseMultiNode(xpath, get_node);
        if (nodes == null) { return null; }

        var actors = [];
        nodes.forEach(function (node) {
            actors.push({ name: node.textContent.substring(1), link: node.href });
        });
        return actors;
    }

    function parseSearchResult() {
        var nodes = _parseMultiNode("//div[@class='photo']/a", get_node);
        if (nodes == null) {
            console.log('no result!');
            CefSharp.PostMessage({ type: 'items', data: 0});
            return;
        }
        if (nodes.length == 1) {
            CefSharp.PostMessage({ type: 'url', data: nodes[0].href});
        }
        else if (nodes.length > 1) {
            console.log('ambiguous!');
        }
    }

    function parseActorPage() {
        var node = _parseSingleNode("//div[@class='profile_picture']", get_node);
        var actor = {};
        var anode = _parseSingleNode("//p[@class='profile_gallery']/img", get_node, node);
        if (anode != null) {
            actor['thumb'] = anode.src;
        }
        var txt = _parseSingleNode("//span[@class='inner_name_kr']", null, node);
        if (txt != null && txt.length > 1) {
            actor['name'] = txt;
        }
        var alias = [];
        txt = _parseSingleNode("//span[@class='inner_name_en']", null, node);
        if (txt != null && txt.length > 1) {
            alias.push(txt);
        }
        txt = _parseSingleNode("//span[@class='inner_name_cn']", null, node);
        if (txt != null && txt.length > 1) {
            var tmp = txt.split('（');
            alias.push(tmp[0]);
        }
/*
        var names = _parseMultiNode("//span[contains(., '다른이름')]/*[contains(@class, 'actor_onm')]");
        if (names != null) {
            names.forEach(function (name) {
                var tmp = name.split(/\(|（/);
                alias.push(tmp[0].substring(1));
                if (tmp.length == 2) {
                    alias.push(tmp[1].substring(0, tmp[1].length - 1));
                }
            });
        }
*/
        //console.log(JSON.stringify(names));

        if (alias.length > 0) {
            actor['alias'] = alias;
        }
        var msg = { type: 'items', data: 1, actor: [actor] };
        console.log(JSON.stringify(msg));
        CefSharp.PostMessage(msg);
    }

    function parseStudio(xpath) {
        var txt = _parseSingleNode(xpath);
        if (txt != null && txt.length > 1) {
            return txt.trim().substring(1);
        }
        return null;
    }

    var parsing_done = false;
    function parsePage() {
        if (parsing_done) {
            console.log('already parsing done!');
            return;
        }
        parsing_done = true;

        var items = {
            title: { xpath: "//div[@class='profile_gallery_text']/span[@id='title_kr']" },
            studio: {
                xpath: "//span[contains(., '제작사:')]/following-sibling::a/text()",
                handler: parseStudio
            },
            series: { xpath: "//span[contains(., '시리즈:')]/following-sibling::text()" },
            date: { xpath: "//span[contains(., '출시:')]/following-sibling::text()" },
            actor_link: {
                xpath: "//span[contains(., '출연:')]/following-sibling::a",
                handler: _parseActor
            },
            genre: { xpath: "//li[@class='gen_list']/a/text()", handler: _parseMultiNode },
        };

        var msg = { type: 'items' }
        var num_item = 0;
        try {
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
        } catch (e) {
            console.log(e.stack);
        }
    }

    if (document.location.href.includes('/menu/actor.php')) {
        parseActorPage();
        return;
    }

    if (document.location.href.includes('/menu/search.php')) {
        window.setTimeout(parseSearchResult, 100);
        //parseSearchResult();
        return;
    }

    var info = document.getElementsByClassName('box');
    if (info.length > 0) {
        parsePage();
    } 
}) ();