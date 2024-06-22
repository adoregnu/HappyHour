(function () {
    const _PID = '{{pid}}';

    function get_node(node) { return node; }

    function parse_actor_page() {
        actor = {};

        var thumb = _jav_parse_single_node("//img[contains(@class, 'act_img')]/@src");
        if (thumb) {
            actor['thumb'] = thumb;
        }
        var names = _jav_parse_multi_node("//div[@class='act_name']/span");
        var alias = _jav_parse_single_node("//span[@class='mv_anotherName']");

        alias.split('＝').forEach(function (an) {
            aname = an.trim();
            aname = aname.split('（')[0];
            aname.split('・').forEach(function (n) {
                if (!names.includes(n)) {
                    names.push(n);
                }
            });
        });
        actor['name'] = names[0];
        alias = [];
        names.forEach(function (n) {
            if (actor['name'].localeCompare(n)) {
                alias.push(n)
            }
        });
        if (alias.length > 0) {
            actor['alias'] = alias;
        }

        var msg = { type: 'items', data: 1, actor: [actor] };
        console.log(JSON.stringify(msg));
        CefSharp.PostMessage(msg);
    }

    function parse_actor(xpath) {
        var nodes = _jav_parse_multi_node(xpath, get_node);
        if (nodes == null) {
            return null;
        }

        var array = [];
        nodes.forEach(function (node) {
            anode = _jav_parse_single_node("div[@class='performer_text']/a", get_node, node);
            array.push({ name: anode.textContent, link: anode.href });
        })

        if (array.length > 0) {
            return array;
        }

        return null;
    }

    function parse_genre(xpath) {
        const excludes = ['ハイビジョン', '4K', '独占配信'];
        var nodes =_jav_parse_multi_node(xpath);
        var genres = [];
        nodes.forEach(function (node) {
            if (!excludes.includes(node)) {
                genres.push(node);
            }
        });

        return genres;
    }

    function parse_search_result() {
        var nodes = _jav_parse_multi_node("//div[@class='movie_info']", get_node);
        if (nodes == null) {
            CefSharp.PostMessage({ type: 'items', data: 0 });
            return;
        }

        var result_url  = null;
        var nmatched = 0;
        for (var i = 0; i < nodes.length; i++) {
            var node = nodes[i]
            var movie_sn = _jav_parse_single_node("div[@class='movie_detail']//span[@class='movie_sn']/img/@src", null, node)
            //console.log(movie_sn)
            if (movie_sn.includes('fanza')) {
                anode = _jav_parse_single_node("div[@class='movie_left']//div[@class='img_wrap']/a", get_node, node);
                nmatched += 1;
                result_url = { type: 'url', data: anode.href };
            }
        }

        if (nmatched == 1) {
            CefSharp.PostMessage(result_url);
        } else {
            console.log('ambiguous');
        }
    }
    function check_search_result() {
        var title = _jav_parse_single_node('//head/title')
        if (title.includes('検索結果')) {
            return true;
        }
        return false;
    }

    if (check_search_result()) {
        parse_search_result();
        return;
    }

    if (document.location.href.includes('/actress')) {
        parse_actor_page();
        return;
    }

    var items = {
        title: { xpath: "//div[@class='mv_title']/text()" },
        date: { xpath: "//a[@class='mv_releaseDate']/text()" },
        studio: { xpath: "//a[@class='mv_label']/text()" },
        cover: { xpath: "//div[@class='movie_top']/img/@src" },
        series: { xpath: "//a[@class='mv_series']"},
        genre: {
            xpath: "//div[@class='mv_genre']/label",
            handler: parse_genre
        },
        actor: {
            xpath: "//div[@class='performer_box']",
            handler: parse_actor
        }
    };

    var result = { type : 'items' }
    var num_item = 0;
    for (var key in items) {
        var item = items[key];
        if (item["handler"] == null) {
            result[key] = _jav_parse_single_node(item['xpath']);
        } else {
            result[key] = item['handler'](item['xpath']);
        }
        if (result[key] == null) {
            continue;
        }
        num_item += 1;
    }
    result['data'] = num_item;
    console.log(JSON.stringify(result));
    CefSharp.PostMessage(result);
}) ();