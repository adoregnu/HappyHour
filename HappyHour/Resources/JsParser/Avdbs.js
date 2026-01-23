(function () {

    g_parse_alias = true;
    function get_node(node) { return node; }
    function parse_actor(xpath) {
        var nodes = _jav_parse_multi_node(xpath, get_node);
        if (nodes == null) { return null; }

        var actors = [];
        nodes.forEach(function (node) {
            var names = node.textContent.substring(1).split(/[(),]/);
            var name = names[0];
            if (names.length > 1) {
                var alias = names.filter(item => name != item && item.length > 0);
                actors.push({ name: name, alias:alias, link: node.href });
            } else {
                actors.push({ name: name, link: node.href });
            }
        });
        return actors;
    }

    function parseSearchResult() {
        var nodes = _jav_parse_multi_node("//div[@class='photo']/a", get_node);
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

    function addAlias(actor, alias, xpath, lang, node) {
        var txt = _jav_parse_single_node(xpath, null, node);
        if (txt != null && txt.length > 1) {
            var tmp = txt.split(/[(（）),、]/);
            if (actor['name'] == null) {
                actor['name'] = tmp[0].trim() + lang;
            } else {
                alias.push(tmp[0].trim() + lang);
            }

            for (var i = 1; i < tmp.length; i++) {
                var name = tmp[i].trim();
                if (name.length > 0) {
                    alias.push(name + lang)
                }
            }
        }
    }

    function parseActorPage() {
        var node = _jav_parse_single_node("//div[@class='profile_picture']", get_node);
        var actor = {};
        var anode = _jav_parse_single_node("p[@class='profile_gallery']/img", get_node, node);
        if (anode != null) {
            actor['thumb'] = anode.src;
        }
        var alias = [];
        addAlias(actor, alias, "//span[@class='inner_name_kr']", ';ko', node);
        addAlias(actor, alias, "//span[@class='inner_name_en']", ';en', node);
        addAlias(actor, alias, "//span[@class='inner_name_cn']", ';jp', node);

        if (g_parse_alias) {
            var names = _jav_parse_multi_node("//span[@class='actor_onm']");
            if (names != null) {
                const checkHan = /[ㄱ-ㅎ|ㅏ-ㅣ|가-힣]/;
                names.forEach(name => {
                    array = name.split(/[()\/#]/).filter(n => n.trim().length > 1);
                    array.forEach((item, idx, arr) => {
                        arr[idx] = checkHan.test(item) ? item.trim() + ';ko' : item.trim() + ';jp';
                    });

                    alias.push(...array);
                });
            }
        }
/*
        var names = _jav_parse_multi_node("//span[contains(., '다른이름')]/*[contains(@class, 'actor_onm')]");
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
        _post_message(msg,'ko');
    }

    function parse_maker(xpath) {
        var txt = _jav_parse_single_node(xpath);
        if (txt != null && txt.length > 1) {
            return txt.trim().substring(1);
        }
        return null;
    }

    function parse_series(xpath) {
        var txt = _jav_parse_single_node(xpath);
        if (txt != null && txt.length > 1) {
            return txt.trim() + ';jp';
        }
        return null;
    }
    function parsePage() {

        var items = {
            title: { xpath: "//div[@class='profile_gallery_text']/span[@id='title_kr']" },
            maker: {
                xpath: "//span[contains(., '제작사:')]/following-sibling::a/text()",
                handler: parse_maker
            },
            label: { xpath: "//span[contains(., '레이블:')]/following-sibling::text()" },
            series: {
                xpath: "//span[contains(., '시리즈:')]/following-sibling::text()",
                handler: parse_series
            },
            date: { xpath: "//span[contains(., '출시:')]/following-sibling::text()" },
            rating: { xpath: "//span[@class='rating']"},
            actor: {
                xpath: "//span[contains(., '출연:')]/following-sibling::a",
                handler: parse_actor
            },
            //plot: { xpath: "//p[@id='story_kr']"},
            genre: { xpath: "//li[@class='gen_list']/a/text()", handler: _jav_parse_multi_node },
        };

        var msg = { type: 'items' }
        var num_item = 0;
        try {
            for (var key in items) {
                var item = items[key];
                if (item["handler"] == null) {
                    msg[key] = _jav_parse_single_node(item['xpath']);
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
            _post_message(msg, 'ko');
        } catch (e) {
            console.log(e.stack);
        }
    }

    if (document.location.href.includes('/menu/actor.php')) {
        parseActorPage();
        return;
    }

    if (document.location.href.includes('/menu/search.php')) {
        window.setTimeout(parseSearchResult, 1000);
        //parseSearchResult();
        //parseSearchResult();
        return;
    }

    var info = document.getElementsByClassName('box');
    if (info.length > 0) {
        parsePage();
    } 
}) ();