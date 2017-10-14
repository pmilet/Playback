﻿// Copyright (c) 2017 Pierre Milet. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using Microsoft.AspNetCore.Http;

namespace pmilet.Playback.Core
{

    public interface IPlaybackContext
    {
        string PlaybackId { get; }
        PlaybackMode PlayBackMode { get; }
        string ContextInfo{ get; set; }
        string RequestBody { get; }
        void Read(HttpContext context, string contextInfoHeader);

    }
}