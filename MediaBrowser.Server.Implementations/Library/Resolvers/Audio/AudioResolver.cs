﻿using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;

namespace MediaBrowser.Server.Implementations.Library.Resolvers.Audio
{
    /// <summary>
    /// Class AudioResolver
    /// </summary>
    public class AudioResolver : ItemResolver<Controller.Entities.Audio.Audio>
    {
        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override ResolverPriority Priority
        {
            get { return ResolverPriority.Last; }
        }

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Entities.Audio.Audio.</returns>
        protected override Controller.Entities.Audio.Audio Resolve(ItemResolveArgs args)
        {
            // Return audio if the path is a file and has a matching extension

            if (!args.IsDirectory)
            {
                if (EntityResolutionHelper.IsAudioFile(args.Path))
                {
                    return new Controller.Entities.Audio.Audio();
                }
            }

            return null;
        }
    }
}
