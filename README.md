# memoQ server Web Services API sample code

This is a minial example to help you get started using memoQ server's Web Service API.
The script-like code in `Program.cs` executes the following tasks:

* Creates a user and sets its password
* Creates a translation memory
* Creates a new project (deleting old project with same name if it exists)
* Assigns the TM to the project
* Uploads a DOCX file in chunks and imports it into the project for translation
* Pre-translates the document
* Calculates a fuzzy analysis and fetches the result as a CSV string
* Assigns the document to the user

## To try the code yourself

Check out the repository and edit two constants at the top of `Program.cs`:
```
const string baseUrl = "https://my-memoq-server.com/memoqservices";
const string apiKey = "<my API key>";
````

Replace the URL with your memoQ server's URL, and insert your own API key. You can find the API key in the Server Administrator:

![logo](https://github.com/memoQ/MemoQAPISample/raw/master/Content/01-server-admin-api-key.png "Server API key")

Running the code:
* On Windows, open the solution in Visual Studio and start debugging
* On Linux:
```
dotnet restore
dotnet run
```

## Nasty .NET Core details

* You need .NET Core 1.0.4 to run the code. 1.0.X and 1.1.X versions are a tremendous version hell, colloquially known as a
classic Microsoft clusterluck.
The solution root includes a `global.json` file, and the project folder includes a `.csproj` file to make things easier.
If all else fails, check out
https://www.microsoft.com/net/download/core and https://jonhilton.net/2017/04/17/making-sense-of-the-different-versions-of-net-core-runtime-and-sdk/
* To generate the files in `Interfaces` you need SvcUtil.exe on Windows. See `GenerateStubs.cmd`. But you can simply use the files
that are included in the solution. They match the API as of memoQ server 8.1.
* In Visual Studio you can supposedly use the "WCF Connected Service" tool from
https://marketplace.visualstudio.com/items?itemName=erikcai-MSFT.VisualStudioWCFConnectedService.
I for my part couldn't get it to work.
* For a great concise guide, check out http://tattoocoder.com/asp-net-core-getting-clean-with-soap/.
* If you're using a different language/platform, you already know how to deal with SOAP APIs by consuming the WSDL returned by the
service. For the exact URIs of the endpoints, see `GenerateStubs.cmd`.

## Don't have a memoQ server at hand?

You can get a free one-month trial at https://www.memoq.com/en/memoq-cloud-server.

Also, give us a holler over any of the channels under [Contact Us](https://www.memoq.com/en/contact-us).
We love to talk about translation and technology.

