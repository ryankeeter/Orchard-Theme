$(document).ready(function() {
    function FirstPostContentOver() {
        $("#firstPostContent").show("slide", { direction: "up" }, 250);
    }
    function FirstPostContentOut() {
        $("#firstPostContent").hide("slide", { direction: "down" }, 250);
    }
    function SecondPostContentOver() {
        $("#secondPostContent").show("slide", { direction: "up" }, 250);
    }
    function SecondPostContentOut() {
        $("#secondPostContent").hide("slide", { direction: "down" }, 250);
    }
    $("#firstPost").hoverIntent(FirstPostContentOver, FirstPostContentOut);
    $("#secondPost").hoverIntent(SecondPostContentOver, SecondPostContentOut);

    //remove Disqus styling
    $("#comments style").remove();
    $("#dsq-combo-widget").attr("class", "custom");
    $("#dsq-combo-tabs").remove();
    $("#dsq-combo-people h3").remove();

    //load external favicons onto links
    $("a[href^='http']").each(function() {
        $(this).css({"background": "url(http://g.etfv.co/" + this.href + ") right center no-repeat",  "padding-right": "40px"});              
        });
    
});