# Asp.Net Core Playback
An Asp.Net Core middleware library that simplifies the recording and playback of api calls by means of a simple playback identifier.
Suitable for saving user interactions in production in order to be replayed locally, anytime and in isolation.

###  How to record and playback Api requests 

Once your Asp.NetCore Api is configured for playback ( see quick start section or sample in github repo ) you can start recording Api requests by setting the X-Playback-Mode header to Record. 

```javascript
curl -X GET --header 'Accept: text/plain' --header 'X-Playback-Mode: Record' 'http://apigatewaysample.azurewebsites.net/api/Hello/hello'
```

then a  x-playback-id response header will be received. 

```javascript
Response Headers
{
  "date": "Wed, 25 Oct 2017 21:05:46 GMT",
  "content-encoding": "gzip",
  "server": "Kestrel",
  "x-powered-by": "ASP.NET",
  "vary": "Accept-Encoding",
  "content-type": "text/plain; charset=utf-8",
  "transfer-encoding": "chunked",
  "x-playback-id": "_ApiGateway+Sample_v1.0_Hello%252Fhello_GET_757602046"
}
```

To replay set the X-Playback-Mode header to Playback and the X-Playback-Id header with the value received from the recording response.

```javascript
curl -X GET --header 'Accept: text/plain' --header 'X-Playback-Id: _ApiGateway+Sample_v1.0_Hello%252Fhello_GET_757602046' --header 'X-Playback-Mode: Playback' 'http://apigatewaysample.azurewebsites.net/api/Hello/bye'
```

When setting the x-playback-mode to None the playback functionality is bypassed. 

### How to Quick Start 

#### in your Startup class:

Configure Playback middleware.

```cs
using pmilet.Playback;

...

public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            ...
            
            services.AddPlayback(Configuration, fakeFactory: new MyPlaybackFakeFactory());
            
            ...
            
            //don't forget to return the service provider
            return services.BuildServiceProvider();

         }
 
 public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            ...
            
            app.UsePlayback();
          
            ...
        }
      
 ...
            
```

#### in your appsetings.json

Add playback storage section

```json
{
  "PlaybackBlobStorage": {
    "ConnectionString": "UseDevelopmentStorage=true",
    "ContainerName": "playback"
  },
```
#### in your controllers
if using swagger, decorate your controller for swagger to generate playback headers in swagger UI  

```cs
using pmilet.Playback;

  ...

  [HttpGet]
  [SwaggerOperationFilter(typeof(PlaybackSwaggerFilter))]
  public async Task<string> Get()
  
  ...
  
```


### How to record responses received from outgoing requests

For recording responses from outgoing requests you should use the PlaybackContext class that can be injected in your api proxies.

this code excerpt show how you can save a response received from an outgoing api call

```cs
       var response = await httpClient.GetAsync(url);
       var result = await response.Content.ReadAsStringAsync();
       if (_playbackContext.IsRecord)
            {
                await _playbackContext.RecordResult<MyServiceResponse>(result);
            }
            else if ( _playbackContext.IsPlayback )
            {
                return await _playbackContext.PlaybackResult<MyServiceResponse>();
            }
     
```

### How to fake api responses 

For faking api call responses implement a class that inherits from IFakeFactory.

in this code excerpt we create a factory by inheriting from the FakeFactoryBase abstract class.

```cs
public class MyPlaybackFakeFactory : FakeFactoryBase
    {
        public override void GenerateFakeResponse(HttpContext context)
        {
            switch (context.Request.Path.Value.ToLower())
            {
                case "/api/hello":
                    if (context.Request.Method == "POST")
                        GenerateFakeResponse<HelloRequest, string>(context, HelloPost);
                    else if (context.Request.Method == "GET")
                        GenerateFakeResponse<string, string>(context, HelloGet);
                    break;
                default:
                    throw new NotImplementedException("fake method not found");
            }
        }
```
Note: this class should be registered in the Startup class IoC Container as IFakeFactory 

