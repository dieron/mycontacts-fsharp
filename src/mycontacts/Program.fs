module mycontacts.App

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe

// ---------------------------------
// Models
// ---------------------------------

type LinkType = 
    | Handle of string
    | Link of string

type Email = {EMailAddress: string} 
type Twitter = {Handle: string}
type Facebook = {LinkType: LinkType; Text: string}
type Instagram = {LinkType: LinkType; Text: string}
type Telegram = {Handle: string}
type Mastodon = {Server: string; Handle: string}
type Skype = {Handle: string}
type Linkedin = {Handle: string; Text: string}
type TextOnly = {Key: string; Value: string}
type Age = {DateOfBirth: DateOnly}
type Nostr = {PublicKey: string}

type Contact =
    | Email of Email
    | Twitter of Twitter
    | Facebook of Facebook
    | Instagram of Instagram 
    | Telegram of Telegram
    | Mastodon of Mastodon
    | Skype of Skype
    | Linkedin of Linkedin
    | Nostr of Nostr
    | TextOnly of TextOnly
    | Age of Age

type ContactsModel = {Contacts: Contact array; Title: string} 


// ---------------------------------
// Views
// ---------------------------------

module Views =
    open Giraffe.ViewEngine

    let layout (titleStr: string) (content: XmlNode list) =
        html [] [
            head [] [
                title []  
                    [
                        if titleStr = null then 
                            encodedText "dieron.com"
                        else
                            encodedText ("dieron.com - " + titleStr) 
                    ]
                link [ _rel  "stylesheet"
                       _type "text/css"
                       _href "/main.css" ]
            ]
            body [] content
        ]

    let partial (t) =
        span [] [
            h1 [] [ encodedText t]
        ]

    let footer () =
        span [_style "font-size: smaller;"] [ 
            encodedText "v. 0.2 "
            a [_href "https://github.com/dieron/mycontacts-fsharp"] [encodedText "github"]
        ]

    let calculateAge (birthDate: DateOnly) (now: DateTime) =
        let age = now.Year - birthDate.Year

        if now.Month < birthDate.Month || (now.Month = birthDate.Month && now.Day < birthDate.Day) then age-1 else age


    let drawSingleContact contact =
        match contact with
            | Email     em -> p [] [b [] [encodedText "E-Mail: "]; a [_href ("mailto:" + em.EMailAddress)] [encodedText em.EMailAddress ] ]
            | Twitter   tw -> p [] [b [] [encodedText "Twitter: "]; a [_href ("https://twitter.com/" + tw.Handle)] [encodedText tw.Handle ] ]
            | Telegram  tl -> p [] [b [] [encodedText "Telegram: "]; a [_href ("https://t.me/" + tl.Handle)] [encodedText tl.Handle ] ]
            | Linkedin  li -> p [] [b [] [encodedText "Linkedin: "]; a [_href ("https://www.linkedin.com/in/" + li.Handle)] [encodedText li.Text ] ]
            | Mastodon   m -> p [] [b [] [encodedText "Mastodon: "]; a [_href ("https://" + m.Server + "/@" + m.Handle)] [encodedText (m.Server + "/@" + m.Handle) ] ]
            | TextOnly   t -> p [] [b [] [encodedText (t.Key + ": ")]; encodedText t.Value ]
            | Skype      s -> p [] [b [] [encodedText "Skype: "]; a [_href ("skype:" + s.Handle)] [encodedText s.Handle ] ]
            | Nostr     ns -> p [] [b [] [encodedText ("Nostr: ")]; encodedText ns.PublicKey ]
            | Age        a -> 
                let age = calculateAge a.DateOfBirth DateTime.Now
                p [] [b [] [encodedText ("Age: ")]; encodedText (age.ToString()) ]

            | Facebook   f ->
                let href = 
                    match f.LinkType with 
                        | Handle h -> "https://www.facebook.com/" + h
                        | Link l -> l

                p [] [b [] [encodedText "Facebook: "]; a [_href href] [encodedText f.Text] ]

            | Instagram  i ->
                let href = 
                    match i.LinkType with 
                        | Handle h -> "https://www.instagram.com/" + h
                        | Link l -> l

                p [] [b [] [encodedText "Instagram: "]; a [_href href] [encodedText i.Text] ]

    let index (model : ContactsModel) =
        [
            partial(model.Title)

            span [] (model.Contacts |> Seq.map (fun c -> drawSingleContact c) |> Seq.toList)  

            footer ()

        ] |> layout model.Title

// ---------------------------------
// Web app
// ---------------------------------

let indexHandler =
    let contacts = 
        {
            Title = "My Info/Contacts"; 
            Contacts = 
                [|
                    TextOnly ({Key = "Name"; Value = "Volodymyr Mishchenko"})
                    Email ({EMailAddress = "dieron@gmail.com"})
                    TextOnly ({Key = "Work"; Value = "Motor Vehicle Software Corporation (MVSC)"})
                    TextOnly ({Key = "Job Title"; Value = "Senior Software Engineer"})
                    Linkedin ({Handle = "dieron"; Text = "Volodymyr Mishchenko"})
                    Twitter ({Handle = "dieron"})
                    Skype ({Handle = "dieron"})
                    Facebook ({LinkType = Handle "dieron"; Text = "Volodymyr Mishchenko"})
                    Instagram ({LinkType = Handle "dieron1"; Text = "@dieron1"})
                    Telegram ({Handle = "dieron1"})
                    Mastodon ({Handle = "dieron"; Server = "mastodon.online"})
                    Nostr ({PublicKey = "npub1zqqchtv854gqtgkz8a98w2kcnt2fysghg42zgedcxxhss65s2e8sh0vhj6"})
                    Age ({DateOfBirth = new DateOnly(1977, 05, 28)})
                |] 
        }

    let view = Views.index contacts

    htmlView view

let webApp =
    choose [
        GET >=>
            choose [
                route "/" >=> indexHandler
            ]
        setStatusCode 404 >=> text "Not Found" ]

// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

// ---------------------------------
// Config and Main
// ---------------------------------

let configureCors (builder : CorsPolicyBuilder) =
    builder
        .WithOrigins(
            "http://localhost:5000",
            "https://localhost:5001")
       .AllowAnyMethod()
       .AllowAnyHeader()
       |> ignore

let configureApp (app : IApplicationBuilder) =
    let env = app.ApplicationServices.GetService<IWebHostEnvironment>()
    (match env.IsDevelopment() with
    | true  ->
        app.UseDeveloperExceptionPage()
    | false ->
        app .UseGiraffeErrorHandler(errorHandler)
            .UseHttpsRedirection())
        .UseCors(configureCors)
        .UseStaticFiles()
        .UseGiraffe(webApp)

let configureServices (services : IServiceCollection) =
    services.AddCors()    |> ignore
    services.AddGiraffe() |> ignore

let configureLogging (builder : ILoggingBuilder) =
    builder.AddConsole()
           .AddDebug() |> ignore

[<EntryPoint>]
let main args =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot     = Path.Combine(contentRoot, "WebRoot")
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(
            fun webHostBuilder ->
                webHostBuilder
                    .UseContentRoot(contentRoot)
                    .UseWebRoot(webRoot)
                    .Configure(Action<IApplicationBuilder> configureApp)
                    .ConfigureServices(configureServices)
                    .ConfigureLogging(configureLogging)
                    |> ignore)
        .Build()
        .Run()
    0