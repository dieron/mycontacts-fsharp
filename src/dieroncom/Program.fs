module dieroncom.App

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

type BaseInfo = {Text: string; Link: string} 

type Email = BaseInfo 
type Twitter = BaseInfo
type Facebook = BaseInfo
type Instagram = BaseInfo
type Telegram = BaseInfo
type Mastodon = BaseInfo
type Skype = BaseInfo
type Linkedin = BaseInfo

type Contacts =
    {
        Title: string

        Name : string
        Work: string
        JobTitle: string
        Email : Email
        Twitter : Twitter
        Skype : Skype
        Mastodon : Mastodon
        Instagram : Instagram
        Facebook : Facebook
        Telegram : Telegram
        Linkedin : Linkedin
        Age: int

    }

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

    let baseInfo info =
        a [_href info.Link] [encodedText info.Text ]

    let partial () =
        h1 [] [ encodedText "My Info/Contacts v.01" ]

    let index (model : Contacts) =
        [
            partial()
            p [] [b [] [encodedText "Name: "]; encodedText model.Name ]
            p [] [b [] [encodedText "E-Mail: "]; baseInfo model.Email ]
            p [] [b [] [encodedText "Work: "]; encodedText model.Work ]
            p [] [b [] [encodedText "Job Title: "]; encodedText model.JobTitle ]
            p [] [b [] [encodedText "Linkedin: "]; baseInfo model.Linkedin ]
            p [] [b [] [encodedText "Twitter: "]; baseInfo model.Twitter ]
            p [] [b [] [encodedText "Facebook: "]; baseInfo model.Facebook ]
            p [] [b [] [encodedText "Instagram: "]; baseInfo model.Instagram ]
            p [] [b [] [encodedText "Telegram: "]; baseInfo model.Telegram ]
            p [] [b [] [encodedText "Mastodon: "]; baseInfo model.Mastodon ]
            p [] [b [] [encodedText "Skype: "]; baseInfo model.Skype ]
            p [] [b [] [encodedText "Age: "]; encodedText (model.Age.ToString()) ]
        ] |> layout model.Title

// ---------------------------------
// Web app
// ---------------------------------

let calculateAge (birthDate: DateTime) (now: DateTime) =
    let age = now.Year - birthDate.Year

    if now.Month < birthDate.Month || (now.Month = birthDate.Month && now.Day < birthDate.Day) then age-1 else age


let indexHandler =
    let model = 
        { 
            Title = "My Info/Contacts"
            Name = "Volodymyr Mishchenko"
            Email = {Text = "dieron@gmail.com"; Link = "mailto:dieron@gmail.com"}
            Skype = {Text = "dieron"; Link = "skype:dieron"}
            Facebook = {Text = "Volodymyr Mishchenko"; Link = "https://www.facebook.com/dieron"}
            Instagram = {Text = "@dieron1"; Link = "https://www.instagram.com/dieron1/"}
            Telegram = {Text = "dieron1"; Link = "https://t.me/dieron1"}
            Mastodon = {Text = "mastodon.online/@dieron"; Link = "https://mastodon.online/@dieron"}
            Twitter = {Text = "@dieron"; Link = "https://twitter.com/dieron"}
            Linkedin = {Text = "Volodymyr Mishchenko"; Link = "https://www.linkedin.com/in/dieron/"}
            JobTitle = "Senior Software Engineer"
            Work = "Motor Vehicle Software Corporation (MVSC)"
            Age = calculateAge (new DateTime(1977, 05, 28)) DateTime.Now
        }
    let view      = Views.index model
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