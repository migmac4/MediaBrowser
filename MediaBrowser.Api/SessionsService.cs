﻿using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Session;
using ServiceStack.ServiceHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class GetSessions
    /// </summary>
    [Route("/Sessions", "GET")]
    [Api(("Gets a list of sessions"))]
    public class GetSessions : IReturn<List<SessionInfoDto>>
    {
        /// <summary>
        /// Gets or sets a value indicating whether [supports remote control].
        /// </summary>
        /// <value><c>null</c> if [supports remote control] contains no value, <c>true</c> if [supports remote control]; otherwise, <c>false</c>.</value>
        [ApiMember(Name = "SupportsRemoteControl", Description = "Optional. Filter by sessions that can be remote controlled.", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool? SupportsRemoteControl { get; set; }
    }

    /// <summary>
    /// Class BrowseTo
    /// </summary>
    [Route("/Sessions/{Id}/Viewing", "POST")]
    [Api(("Instructs a session to browse to an item or view"))]
    public class BrowseTo : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Session Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public Guid Id { get; set; }

        /// <summary>
        /// Artist, Genre, Studio, Person, or any kind of BaseItem
        /// </summary>
        /// <value>The type of the item.</value>
        [ApiMember(Name = "ItemType", Description = "The type of item to browse to.", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string ItemType { get; set; }

        /// <summary>
        /// Artist name, genre name, item Id, etc
        /// </summary>
        /// <value>The item identifier.</value>
        [ApiMember(Name = "ItemId", Description = "The Id of the item.", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string ItemId { get; set; }

        /// <summary>
        /// Gets or sets the name of the item.
        /// </summary>
        /// <value>The name of the item.</value>
        [ApiMember(Name = "ItemName", Description = "The name of the item.", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string ItemName { get; set; }

        /// <summary>
        /// Gets or sets the context (Movies, Music, TvShows, etc)
        /// Applicable to genres, studios and persons only because the context of items and artists can be inferred.
        /// This is optional to supply and clients are free to ignore it.
        /// </summary>
        /// <value>The context.</value>
        [ApiMember(Name = "Context", Description = "The ui context for the client (movies, music, tv, games etc). This is optional to supply and clients are free to ignore it.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Context { get; set; }
    }

    [Route("/Sessions/{Id}/Playing", "POST")]
    [Api(("Instructs a session to play an item"))]
    public class Play : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Session Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public Guid Id { get; set; }

        /// <summary>
        /// Artist, Genre, Studio, Person, or any kind of BaseItem
        /// </summary>
        /// <value>The type of the item.</value>
        [ApiMember(Name = "ItemIds", Description = "The ids of the items to play, comma delimited", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST", AllowMultiple = true)]
        public string ItemIds { get; set; }

        /// <summary>
        /// Gets or sets the start position ticks that the first item should be played at
        /// </summary>
        /// <value>The start position ticks.</value>
        [ApiMember(Name = "StartPositionTicks", Description = "The starting position of the first item.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public long? StartPositionTicks { get; set; }

        /// <summary>
        /// Gets or sets the play command.
        /// </summary>
        /// <value>The play command.</value>
        [ApiMember(Name = "PlayCommand", Description = "The type of play command to issue (PlayNow, PlayNext, PlayLast). Clients who have not yet implemented play next and play last may play now.", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public PlayCommand PlayCommand { get; set; }
    }

    /// <summary>
    /// Class SessionsService
    /// </summary>
    public class SessionsService : BaseApiService
    {
        /// <summary>
        /// The _session manager
        /// </summary>
        private readonly ISessionManager _sessionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionsService" /> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        public SessionsService(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetSessions request)
        {
            var result = _sessionManager.Sessions.Where(i => i.IsActive);

            if (request.SupportsRemoteControl.HasValue)
            {
                result = result.Where(i => i.IsActive == request.SupportsRemoteControl.Value);
            }

            return ToOptimizedResult(result.Select(SessionInfoDtoBuilder.GetSessionInfoDto).ToList());
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(BrowseTo request)
        {
            var task = BrowseTo(request);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Browses to.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ResourceNotFoundException"></exception>
        /// <exception cref="System.ArgumentException"></exception>
        /// <exception cref="System.InvalidOperationException">The requested session does not have an open web socket.</exception>
        private async Task BrowseTo(BrowseTo request)
        {
            var session = _sessionManager.Sessions.FirstOrDefault(i => i.Id == request.Id);

            if (session == null)
            {
                throw new ResourceNotFoundException(string.Format("Session {0} not found.", request.Id));
            }

            if (!session.SupportsRemoteControl)
            {
                throw new ArgumentException(string.Format("Session {0} does not support remote control.", session.Id));
            }

            var socket = session.WebSockets.OrderByDescending(i => i.LastActivityDate).FirstOrDefault(i => i.State == WebSocketState.Open);

            if (socket != null)
            {
                try
                {
                    await socket.SendAsync(new WebSocketMessage<BrowseTo>
                    {
                        MessageType = "Browse",
                        Data = request

                    }, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error sending web socket message", ex);
                }
            }
            else
            {
                throw new InvalidOperationException("The requested session does not have an open web socket.");
            }
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(Play request)
        {
            var task = Play(request);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Plays the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ResourceNotFoundException"></exception>
        /// <exception cref="System.ArgumentException"></exception>
        /// <exception cref="System.InvalidOperationException">The requested session does not have an open web socket.</exception>
        private async Task Play(Play request)
        {
            var session = _sessionManager.Sessions.FirstOrDefault(i => i.Id == request.Id);

            if (session == null)
            {
                throw new ResourceNotFoundException(string.Format("Session {0} not found.", request.Id));
            }

            if (!session.SupportsRemoteControl)
            {
                throw new ArgumentException(string.Format("Session {0} does not support remote control.", session.Id));
            }

            var socket = session.WebSockets.OrderByDescending(i => i.LastActivityDate).FirstOrDefault(i => i.State == WebSocketState.Open);

            if (socket != null)
            {
                try
                {
                    await socket.SendAsync(new WebSocketMessage<PlayRequest>
                    {
                        MessageType = "Play",

                        Data = new PlayRequest
                        {
                            ItemIds = request.ItemIds.Split(',').ToArray(),

                            PlayCommand = request.PlayCommand,
                            StartPositionTicks = request.StartPositionTicks
                        }

                    }, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error sending web socket message", ex);
                }
            }
            else
            {
                throw new InvalidOperationException("The requested session does not have an open web socket.");
            }
        }
    }
}
