﻿// Copyright (c) 2017 Pierre Milet. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using pmilet.Playback.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace pmilet.Playback
{
    public class PlaybackMiddleware 
    {
        private readonly IFakeFactory _fakeFactory;
        private readonly IPlaybackStorageService _messageStorageService;
        protected readonly RequestDelegate _next;
        private readonly IPlaybackContext _playbackContext;

        public PlaybackMiddleware(RequestDelegate next, IFakeFactory fakeFactory, IPlaybackStorageService messageStorageService, IPlaybackContext playbackContext)            
        {
            _fakeFactory = fakeFactory;
            _messageStorageService = messageStorageService;
            _next = next;
            _playbackContext = playbackContext;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            _playbackContext.Read(httpContext );

            httpContext.Request.EnableRewind();
            switch (_playbackContext.PlaybackMode)
            {
                case PlaybackMode.Fake:
                    _fakeFactory.GenerateFakeResponse(httpContext);
                    if (httpContext.Response.Body == null)
                        await _next.Invoke(httpContext);
                    break;
                case PlaybackMode.Grabacion:
                    var pathDecode = WebUtility.UrlDecode(httpContext.Request.Path);
                    await _messageStorageService.UploadToStorageAsync(_playbackContext.PlaybackId, pathDecode, httpContext.Request.QueryString.Value, _playbackContext.Content);
                    httpContext.Request.Body.Position = 0;
                    httpContext.Response.OnStarting(state => {
                        var httpContextState = (HttpContext)state;
                        httpContextState.Response.Headers.Add("PlaybackId", new[] { _playbackContext.PlaybackId });
                        return Task.FromResult(0);
                    }, httpContext);
                    await _next.Invoke(httpContext);
                    break;
                case PlaybackMode.Playback:
                case PlaybackMode.PlaybackReal:
                case PlaybackMode.PlaybackChaos:
                    if (!string.IsNullOrWhiteSpace(_playbackContext.PlaybackId))
                    {
                        PlaybackMessage playbackMessage = await _messageStorageService.DownloadFromStorageAsync(_playbackContext.PlaybackId);
                        httpContext.Request.Body = playbackMessage.GetBodyStream();
                        httpContext.Request.QueryString = new QueryString(playbackMessage.QueryString);
                        var path = WebUtility.UrlDecode(playbackMessage.Path);
                        httpContext.Request.Path = path;
                    }
                    await _next.Invoke(httpContext);
                    break;
                default:
                    await _next.Invoke(httpContext);
                    break;
            }
        }
    }
}
