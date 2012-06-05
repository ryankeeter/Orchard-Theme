 $(document).ready(function(){
    function FirstPostContentOver(){
            $("#firstPostContent").show("slide", {direction: "up"}, 250);
    }
    function FirstPostContentOut(){
             $("#firstPostContent").hide("slide", {direction: "down"}, 250);
    }
     function SecondPostContentOver(){
            $("#secondPostContent").show("slide", {direction: "up"}, 250);
    }
    function SecondPostContentOut(){
             $("#secondPostContent").hide("slide", {direction: "down"}, 250);
    }
    $("#firstPost").hoverIntent(FirstPostContentOver, FirstPostContentOut);
    $("#secondPost").hoverIntent(SecondPostContentOver, SecondPostContentOut);
});