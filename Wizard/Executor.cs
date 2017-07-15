﻿/*
 * The MIT License (MIT)
 * 
 * Copyright (c) 2016-2017  Denis Kuzmin < entry.reg@gmail.com > :: github.com/3F
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using net.r_eg.DllExport.NSBin;
using net.r_eg.MvsSln;
using net.r_eg.MvsSln.Core;
using net.r_eg.MvsSln.Log;

namespace net.r_eg.DllExport.Wizard
{
    public class Executor: IExecutor, IDisposable
    {
        protected Dictionary<string, Sln> solutions = new Dictionary<string, Sln>();

        /// <summary>
        /// Access to wizard configuration.
        /// </summary>
        public IWizardConfig Config
        {
            get;
            protected set;
        }

        /// <summary>
        /// ddNS feature core.
        /// </summary>
        public IDDNS DDNS
        {
            get;
            set;
        } = new DDNS(Encoding.UTF8);

        /// <summary>
        /// List of available .sln files.
        /// </summary>
        public IEnumerable<string> SlnFiles
        {
            get {
                return Directory.GetFiles(Config.SlnDir, "*.sln", SearchOption.TopDirectoryOnly)
                                .Select(s => Path.GetFullPath(s));
            }
        }

        /// <summary>
        /// List of all found projects with different configurations.
        /// </summary>
        /// <param name="sln">Full path to .sln</param>
        /// <returns></returns>
        public IEnumerable<IProject> ProjectsBy(string sln)
        {
            return GetEnv(sln)?.Projects.Select(p => new Project(p, GetDefaultUserConfig()));
        }

        /// <summary>
        /// List of all found projects that's unique by guid.
        /// </summary>
        /// <param name="sln"></param>
        /// <returns></returns>
        public IEnumerable<IProject> UniqueProjectsBy(string sln)
        {
            return GetEnv(sln)?.UniqueByGuidProjects.Select(p => new Project(p, GetDefaultUserConfig()));
        }

        /// <summary>
        /// Access to logger.
        /// </summary>
        public ISender Log
        {
            get => LSender._;
        }

        /// <summary>
        /// To start process of the required configuration.
        /// </summary>
        public void Configure()
        {
            if(Config.Type == ActionType.Configure) {
                var frm = new UI.ConfiguratorForm(this);
                frm.ShowDialog();
            }

            if(Config.Type == ActionType.Restore) {
                //TODO:
            }
        }

        /// <param name="cfg"></param>
        public Executor(IWizardConfig cfg)
        {
            Config = cfg ?? throw new ArgumentNullException(nameof(cfg));
        }

        protected virtual IUserConfig GetDefaultUserConfig()
        {
            return new UserConfig(Config) {
                NSBuffer    = DDNS.NSBuffer,
                DDNS        = DDNS,
                Log         = Log,
            };
        }

        protected IEnvironment GetEnv(string file)
        {
            if(String.IsNullOrWhiteSpace(file)) {
                return null;
            }

            if(!solutions.ContainsKey(file)) {
                solutions[file] = new Sln(file, SlnItems.EnvWithProjects);
            }
            return solutions[file].Result.Env;
        }

        private void Free()
        {
            if(solutions == null) {
                return;
            }

            foreach(var sln in solutions) {
                sln.Value.Result?.Env?.Dispose();
            }
        }

        #region IDisposable

        // To detect redundant calls
        private bool disposed = false;

        // To correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if(disposed) {
                return;
            }
            disposed = true;

            Free();
        }

        #endregion
    }
}