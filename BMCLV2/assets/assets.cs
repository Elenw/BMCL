﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Web.Script.Serialization;
using System.IO;

using BMCLV2.util;

namespace BMCLV2.Assets
{
    public class Assets
    {
        private readonly WebClient _downloader = new WebClient();
        bool _init = true;
        readonly gameinfo _gameInfo;
        Dictionary<string, string> _downloadUrlPathPair = new Dictionary<string, string>();
        private readonly string _urlDownloadBase;
        private readonly string _urlResourceBase;
        public Assets(gameinfo gameInfo, string urlDownloadBase = null, string urlResourceBase = null)
        {
            this._gameInfo = gameInfo;
            this._urlDownloadBase = urlDownloadBase ?? BmclCore.UrlDownloadBase;
            this._urlResourceBase = urlResourceBase ?? BmclCore.UrlResourceBase;
            var thread = new Thread(Run);
            thread.Start();
        }

        private void Run()
        {
            string gameVersion = _gameInfo.assets;
            try
            {
                _downloader.DownloadStringAsync(new Uri(_urlDownloadBase + "indexes/" + gameVersion + ".json"));
                Logger.info(_urlDownloadBase + "indexes/" + gameVersion + ".json");
            }
            catch (WebException ex)
            {
                Logger.info("游戏版本" + gameVersion);
                Logger.error(ex);
            }
            _downloader.DownloadStringCompleted += Downloader_DownloadStringCompleted;
            _downloader.DownloadFileCompleted += Downloader_DownloadFileCompleted;
        }
        void Downloader_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                Logger.error(e.UserState.ToString());
                Logger.error(e.Error);
            }
        }

        void Downloader_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            _downloader.DownloadStringCompleted -= Downloader_DownloadStringCompleted;
            if (e.Error != null)
            {
                if (e.Error is WebException)
                {
                    var ex = e.Error as WebException;
                    Logger.log(ex.Response.ResponseUri.ToString());
                }
                Logger.error(e.Error);
            }
            else
            {
                string gameVersion = _gameInfo.assets;
                FileHelper.CreateDirectoryForFile(AppDomain.CurrentDomain.BaseDirectory + ".minecraft/assets/indexes/" + gameVersion + ".json");
                var sw = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + ".minecraft/assets/indexes/" + gameVersion + ".json");
                sw.Write(e.Result);
                sw.Close();
                var jsSerializer = new JavaScriptSerializer();
                var assetsObject = jsSerializer.Deserialize<Dictionary<string, Dictionary<string, AssetsEntity>>>(e.Result);
                Dictionary<string, AssetsEntity> obj = assetsObject["objects"];
                Logger.log("共", obj.Count.ToString(CultureInfo.InvariantCulture), "项assets");
                int i = 0;
                foreach (KeyValuePair<string, AssetsEntity> entity in obj)
                {
                    i++;
                    string url = this._urlResourceBase + entity.Value.hash.Substring(0, 2) + "/" + entity.Value.hash;
                    string file = AppDomain.CurrentDomain.BaseDirectory + @".minecraft\assets\objects\" + entity.Value.hash.Substring(0, 2) + "\\" + entity.Value.hash;
                    FileHelper.CreateDirectoryForFile(file);
                    try
                    {
                        if (FileHelper.IfFileVaild(file, entity.Value.size)) continue;
                        if (_init)
                        {
                            BmclCore.NIcon.ShowBalloonTip(3000, Lang.LangManager.GetLangFromResource("FoundAssetsModify"));
                            _init = false;
                        }
                        //Downloader.DownloadFileAsync(new Uri(Url), File,Url);
                        _downloader.DownloadFile(new Uri(url), file);
                        Logger.log(i.ToString(CultureInfo.InvariantCulture), "/", obj.Count.ToString(CultureInfo.InvariantCulture), file.Substring(AppDomain.CurrentDomain.BaseDirectory.Length), "下载完毕");
                        if (i == obj.Count)
                        {
                            Logger.log("assets下载完毕");
                            BmclCore.NIcon.ShowBalloonTip(3000, Lang.LangManager.GetLangFromResource("SyncAssetsFinish"));
                        }
                    }
                    catch (WebException ex)
                    {
                        Logger.log(ex.Response.ResponseUri.ToString());
                        Logger.error(ex);
                    }
                }
                if (_init)
                {
                    Logger.info("无需更新assets");
                }
            }
            
        }
    }
}
