(function () {
    const _PID = '{{pid}}';
    function parseSearchResult() {
        var urls = _jav_parse_multi_node("//a[@class='movie-box']/@href");
        if (urls == null) {
            //CefSharp.PostMessage({ type: 'items', data: 0 });
            _post_message({ type: 'items', data: 0 }, 'jp');
        } else if (urls.length == 1) {
            //CefSharp.PostMessage({ type: 'url', data: urls[0] });
            _post_message({ type: 'url', data: urls[0] }, 'jp');
        } else {
            console.log('ambiguous result!');
        }
    }

    function get_node(node) { return node; }

    function _parse_cover(xpath) {
        var img = _jav_parse_single_node(xpath, get_node);
        if (img != null) {
            var name = img.src.split('/').pop();
            return {
                img_url: img.src,
                target: name,
                func: () => {
                    const link = document.createElement('a');
                    document.body.appendChild(link);
                    link.download = name;
                    link.href = img.src;
                    link.target = '_blank';
                    link.click();
                    document.body.removeChild(link);
                }
            };
        }
        return null;
    }

    function parseActor(xpath) {
        var result = _jav_parse_multi_node(xpath, get_node);
        if (result == null) {
            console.log("no actors");
            return null;
        }
        //console.log(result.length + " actors");
        var actors = [];
        result.forEach(img => {
            if (!actors.some(a => a['name'] == img.title.trim())) {
                actors.push({name: img.title.trim()})
            }
            /*
            if (!img.src.includes('nowprinting')) {
                var name = img.src.split('/').pop();
                actor['thumb'] = {
                    img_url: img.src,
                    target: name,
                    func: () => {
                        console.log(img.src);
                        const link = document.createElement('a');
                        document.body.appendChild(link);
                        link.download = name;
                        link.href = img.src;
                        link.target = '_blank';
                        link.click();
                        document.body.removeChild(link);
                    }
                };
            }
            */
        });

        return actors;
    }

    function parse_genre(xpath) {
        var nodes = _jav_parse_multi_node(xpath, get_node);
        if (nodes == null) {
            return null;
        }
        var excludes = ['AV女優', '1080p', '60fps', '超VIP', 'Hi-Def', '4K', 'ハイビジョン', '独占配信'];
        var result = nodes.filter(n => !excludes.some(ex => ex == n.textContent.trim()) && n.href.includes('genre'));

        var genre = [];
        result.forEach(r => {
            genre.push(r.textContent.trim());
        });
        return genre;
    }

    if (document.location.href.includes('/search/')) {
        parseSearchResult();
        return;
    }

    var items = {
        title: { xpath: "//div[@class='container']/h3/text()" },
        cover: { xpath: "//a[@class='bigImage']/img", handler: _parse_cover },
        date: { xpath: "//span[contains(.,'発売日:')]/following-sibling::text()" },
        maker: { xpath: "//span[contains(.,'メーカー:')]/following-sibling::a/text()" },
        label: { xpath: "//span[contains(.,'レーベル:')]/following-sibling::a/text()" },
        series: { xpath: "//span[contains(.,'シリーズ:')]/following-sibling::a/text()" },
        genre: {
            xpath: "//p[contains(.,'ジャンル:')]/following-sibling::p/span//a",
            handler: parse_genre
        },
        actor: { xpath: "//div[@id='avatar-waterfall']//img", handler: parseActor }
    };

    var msg = { type: 'items' }
    var num_item = 0;
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
    _post_message(msg, 'jp');
})();