namespace MySPA

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Client
open WebSharper.UI.Templating
open WebSharper.Sitelets

// The templates are loaded from the DOM, so you just can edit index.html
// and refresh your browser, no need to recompile unless you add or remove holes.
type IndexTemplate = Template<"wwwroot/index.html", ClientLoad.FromDocument>

// Our SPA endpoints
type EndPoint =
    | [<EndPoint "/">] Home
    | [<EndPoint "/counter">] Counter

[<JavaScript>]
module Pages =
    open WebSharper.UI.Notation
    open WebSharper.JavaScript

    let People =
        ListModel.FromSeq [
            "John"
            "Paul"
        ]

    let HomePage() =
        let newName = Var.Create ""
        IndexTemplate.HomePage()
            // This is where we would instantiate placeholders
            // and bind event handlers in our template.
            .ListContainer(
                People.View.DocSeqCached(fun (name: string) ->
                    IndexTemplate.ListItem().Name(name).Doc()
                )
            )
            .Name(newName)
            .Add(fun _ ->
                People.Add(newName.Value)
                newName.Value <- ""
            )
            .Doc()

    let storage = JS.Window.LocalStorage
    let counter =
        let curr = storage.GetItem "counter"
        if curr = "" then
            0
        else
            int curr
        |> Var.Create

    let CounterPage() =
        IndexTemplate.CounterPage()
            .Value(View.Map string counter.View)
            .Decrement(fun e ->
                counter := counter.Value - 1
                storage.SetItem("counter", string counter.Value)
            )
            .Increment(fun e ->
                counter := counter.Value + 1
                storage.SetItem("counter", string counter.Value)
            )
            .Doc()

[<JavaScript>]
module App =
    open WebSharper.UI.Notation

    // Create a router for our endpoints
    let router = Router.Infer<EndPoint>()
    // Install our client-side router and track the current page
    let currentPage = Router.InstallHash Home router

    type Router<'T when 'T: equality> with
        member this.LinkHash (ep: 'T) = "/#" + this.Link ep

    [<SPAEntryPoint>]
    let Main () =
        let renderInnerPage (currentPage: Var<EndPoint>) =
            currentPage.View.Map (fun endpoint ->
                match endpoint with
                | Home      -> Pages.HomePage()
                | Counter   -> Pages.CounterPage()
            )
            |> Doc.EmbedView
        
        IndexTemplate()
            .Url_Home(router.LinkHash EndPoint.Home)
            .Url_Page1(router.LinkHash EndPoint.Counter)
            .TakeMeHome(fun e ->
                currentPage := EndPoint.Home
            )
            .MainContainer(renderInnerPage currentPage)
            .Bind()
