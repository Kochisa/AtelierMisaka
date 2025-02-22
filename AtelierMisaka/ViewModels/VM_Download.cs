using AtelierMisaka.Commands;
using AtelierMisaka.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AtelierMisaka.ViewModels
{
    public class VM_Download : NotifyModel
    {
        private string _exportFile = Path.GetDirectoryName(System.Reflection.Assembly.GetCallingAssembly().Location) + "\\Export_Errordownload.txt";
        private bool _canExport = true;

        private bool _isDownloading = false;
        private bool _isChangeThread = false;
        private bool _isChangeProxy = false;
        private bool _isFantia = false;
        private bool _showCheck = false;
        private bool _isQuest = false;
        private string _savePath = string.Empty;
        private string _tempAI = string.Empty;
        private string _tempAN = string.Empty;
        private int _threadCount = 3;
        private double _mLeft = 0d;
        private double _mTop = 0d;
        private SiteType _tempSite = SiteType.Fanbox;

        private List<DownloadItem> _dlClients = new List<DownloadItem>();

        private IList<DownloadItem> _downLoadList = null;
        private IList<DownloadItem> _completedList = null;

        private HashSet<string> _retryList = null;

        private static readonly object lock_Dl = new object();
        private static readonly object lock_DList = new object();
        private static readonly object lock_Fantia = new object();
        private readonly DownloadStatus[] dsArr = new DownloadStatus[] { DownloadStatus.Downloading, DownloadStatus.Waiting, DownloadStatus.WriteFile };

        public int ThreadCount
        {
            get => _threadCount;
            set
            {
                if (_threadCount != value)
                {
                    _threadCount = value;
                    RaisePropertyChanged();
                    if (_isDownloading)
                    {
                        lock (lock_Dl)
                        {
                            if (_dlClients.Count > _threadCount)
                            {
                                for (int i = _dlClients.Count - 1; i >= _threadCount; i++)
                                {
                                    _dlClients[i].Pause();
                                    _dlClients.RemoveAt(i);
                                }
                            }
                            else
                            {
                                for (int i = 0; i < _downLoadList.Count; i++)
                                {
                                    if (_downLoadList[i].DLStatus == DownloadStatus.Waiting)
                                    {
                                        if (!_dlClients.Contains(_downLoadList[i]))
                                        {
                                            _dlClients.Add(_downLoadList[i]);
                                        }
                                        _downLoadList[i].Start();
                                        if (_dlClients.Count >= _threadCount)
                                        {
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public bool UpdateCul
        {
            set
            {
                RaisePropertyChanged("BtnText");
            }
        }

        public string BtnText
        {
            get => _isDownloading ? GlobalLanguage.Text_AllPause : GlobalLanguage.Text_AllStart;
        }

        public string SavePath
        {
            get => _savePath;
            set
            {
                if (_savePath != value)
                {
                    _savePath = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string TempAI
        {
            get => _tempAI;
            set
            {
                if (_tempAI != value)
                {
                    _tempAI = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string TempAN
        {
            get => _tempAN;
            set
            {
                if (_tempAN != value)
                {
                    _tempAN = value;
                    RaisePropertyChanged();
                }
            }
        }

        public SiteType TempSite
        {
            get => _tempSite;
            set
            {
                if (_tempSite != value)
                {
                    _tempSite = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool ShowCheck
        {
            get => _showCheck;
            set
            {
                if (_showCheck != value)
                {
                    _showCheck = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool IsDownloading
        {
            get => _isDownloading;
            set
            {
                if (_isDownloading != value)
                {
                    _isDownloading = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged("BtnText");
                }
            }
        }

        public bool WaitDownloading
        {
            get
            {
                int cou = 0;
                lock (lock_DList)
                {
                    for (int i = 0; i < _downLoadList.Count; i++)
                    {
                        if (dsArr.Contains(_downLoadList[i].DLStatus))
                        {
                            cou++;
                        }
                    }
                }
                return cou >= _threadCount;
            }
        }

        public bool IsChangeThread
        {
            get => _isChangeThread;
            set
            {
                if (_isChangeThread != value)
                {
                    _isChangeThread = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool IsChangeProxy
        {
            get => _isChangeProxy;
            set
            {
                if (_isChangeProxy != value)
                {
                    _isChangeProxy = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool IsFantia
        {
            get => _isFantia;
            set
            {
                if (_isFantia != value)
                {
                    _isFantia = value;
                    if (_isFantia)
                    {
                        _retryList = new HashSet<string>();
                    }
                }
            }
        }

        public double MLeft
        {
            get => _mLeft;
            set
            {
                if (_mLeft != value)
                {
                    _mLeft = value;
                    RaisePropertyChanged();
                }
            }
        }

        public double MTop
        {
            get => _mTop;
            set
            {
                if (_mTop != value)
                {
                    _mTop = value;
                    RaisePropertyChanged();
                }
            }
        }

        public List<DownloadItem> DLClients
        {
            get => _dlClients;
            set
            {
                if (_dlClients != value)
                {
                    _dlClients = value;
                    RaisePropertyChanged();
                }
            }
        }

        public IList<DownloadItem> DownLoadItemList
        {
            get => _downLoadList;
            set
            {
                if (_downLoadList != value)
                {
                    _downLoadList = value;
                    RaisePropertyChanged();
                }
            }
        }

        public IList<DownloadItem> CompletedList
        {
            get => _completedList;
            set
            {
                if (_completedList != value)
                {
                    _completedList = value;
                    RaisePropertyChanged();
                }
            }
        }

        public CommonCommand QuestCommand
        {
            get => new CommonCommand(async() =>
            {
                _isQuest = true;
                await Task.Run(() =>
                {
                    IsDownloading = !_isDownloading;
                    lock (lock_Dl)
                    {
                        if (_isDownloading)
                        {
                            for (int i = 0; i < _downLoadList.Count; i++)
                            {
                                if (_downLoadList[i].DLStatus == DownloadStatus.Waiting)
                                {
                                    if (!_dlClients.Contains(_downLoadList[i]))
                                    {
                                        _dlClients.Add(_downLoadList[i]);
                                    }
                                    _downLoadList[i].Start();
                                    if (_dlClients.Count >= _threadCount)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            _dlClients.ForEach(x => x.Pause());
                            _dlClients.Clear();
                        }
                    }
                    if (GlobalData.VM_MA.ShowLoad)
                    {
                        return;
                    }
                    if (_isDownloading)
                    {
                        var dl = _downLoadList.ToList();
                        if (dl.FindIndex(x => x.DLStatus != DownloadStatus.Cancel) == -1)
                        {
                            IsDownloading = false;
                            if (string.IsNullOrEmpty(GlobalData.VM_MA.Date_End))
                            {
                                GlobalData.VM_MA.Date_Start = GlobalData.StartTime.ToString("yyyy/MM/dd HH:mm:ss");
                                GlobalData.LastDateDic.Update(GlobalData.VM_MA.LastDate);
                            }
                            else
                            {
                                GlobalData.VM_MA.LastDate = GlobalData.VM_MA.LastDate_End;
                                GlobalData.LastDateDic.Update(GlobalData.VM_MA.LastDate_End);
                            }
                        }
                    }
                });
                _isQuest = false;
            },
            () =>
            {
                return !_isQuest && _downLoadList != null && _downLoadList.Count > 0;
            });
        }

        public CommonCommand ExportCommand
        {
            get => new CommonCommand(() =>
            {
                try
                {
                    if (File.Exists(_exportFile))
                    {
                        File.Delete(_exportFile);
                    }
                    for (int i = 0; i < _downLoadList.Count; i++)
                    {
                        if (_downLoadList[i].DLStatus == DownloadStatus.Error)
                        {
                            GlobalMethord.ExportErrorDownload(_downLoadList[i]);
                        }
                    }
                    if (File.Exists(_exportFile))
                    {
                        System.Diagnostics.Process.Start(_exportFile);
                    }
                }
                catch (Exception ex)
                {
                    GlobalMethord.ErrorLog(ex.Message);
                    _canExport = false;
                }
            },
            () => { return _canExport && !_isDownloading; });
        }

        public ParamCommand<DownloadItem> DownloadCommand
        {
            get => new ParamCommand<DownloadItem>((di) =>
            {
                if (di.DLStatus == DownloadStatus.Downloading)
                {
                    PauseCommand.Execute(di);
                }
                else if (di.DLStatus == DownloadStatus.Paused || di.DLStatus == DownloadStatus.Waiting)
                {
                    StartCommand.Execute(di);
                }
            });
        }

        public ParamCommand<DownloadItem> OptionCommand
        {
            get => new ParamCommand<DownloadItem>((di) =>
            {
                switch (di.DLStatus)
                {
                    case DownloadStatus.Waiting:
                        ToFirstCommand.Execute(di);
                        break;
                    case DownloadStatus.Cancel:
                        ReStartCommand.Execute(di);
                        break;
                    case DownloadStatus.Common:
                        CancelCommand.Execute(di);
                        break;
                    default:
                        ReStartCommand.Execute(di);
                        break;
                }
            });
        }

        public ParamCommand<DownloadItem> MoveToComLCommand
        {
            get => new ParamCommand<DownloadItem>((di) =>
            {
                lock (lock_DList)
                {
                    _downLoadList.Remove(di);
                    _completedList.Insert(0, di);
                }
            });
        }

        public ParamCommand<object[]> AddCommand
        {
            get => new ParamCommand<object[]>((args) =>
            {
                DownloadItem di = null;
                int index = (int)args[2];
                switch (GlobalData.VM_MA.Site)
                {
                    case SiteType.Fanbox:
                        {
                            BaseItem bi = (BaseItem)args[1];
                            {
                                string sp = Path.Combine(_savePath, _tempAN, $"{bi.CreateDate.ToString("yyyy-MM-dd-HH")}_{bi.Title}_{bi.Fee}");
                                try
                                {
                                    Directory.CreateDirectory(sp);
                                }
                                catch
                                {
                                    if (!Directory.Exists(sp))
                                    {
                                        sp = GlobalMethord.RemoveLastDot(GlobalMethord.ReplacePath(sp));
                                        Directory.CreateDirectory(sp);
                                    }
                                }
                                if (index == -1)
                                {
                                    di = new DownloadItem
                                    {
                                        FileName = bi.CoverPicName,
                                        Link = bi.CoverPic,
                                        SavePath = sp,
                                        CTime = bi.CreateDate,
                                        SourceDocu = bi,
                                        AId = _tempAI
                                    };
                                    di.CheckTempFile();
                                }
                                else
                                {
                                    if ((bool)args[0])
                                    {
                                        di = new DownloadItem
                                        {
                                            FileName = bi.FileNames[index],
                                            Link = bi.ContentUrls[index],
                                            SavePath = sp,
                                            CTime = bi.CreateDate,
                                            SourceDocu = bi,
                                            AId = _tempAI
                                        };
                                        di.CheckTempFile();
                                    }
                                    else
                                    {
                                        di = new DownloadItem
                                        {
                                            FileName = bi.MediaNames[index],
                                            Link = bi.MediaUrls[index],
                                            SavePath = sp,
                                            CTime = bi.CreateDate,
                                            SourceDocu = bi,
                                            AId = _tempAI
                                        };
                                        di.CheckTempFile();
                                    }
                                }
                                GlobalData.VM_DL.DownLoadItemList.Add(di);
                            }
                        }
                        break;
                    case SiteType.Fantia:
                        {
                            FantiaItem fi = (FantiaItem)args[1];
                            string sp = Path.Combine(_savePath, _tempAN, $"{fi.CreateDate.ToString("yyyy-MM-dd-HH")}_{fi.Title}");
                            try
                            {
                                Directory.CreateDirectory(sp);
                            }
                            catch
                            {
                                if (!Directory.Exists(sp))
                                {
                                    sp = GlobalMethord.RemoveLastDot(GlobalMethord.ReplacePath(sp));
                                    Directory.CreateDirectory(sp);
                                }
                            }
                            if (index == -1)
                            {
                                di = new DownloadItem
                                {
                                    FileName = fi.CoverPicName,
                                    Link = fi.CoverPic,
                                    SavePath = sp,
                                    SourceDocu = fi,
                                    AId = _tempAI
                                };
                                di.CheckTempFile();
                            }
                            else
                            {
                                var nsp = $"{sp}\\{fi.PTitles[index]}";
                                if (!Directory.Exists(nsp))
                                {
                                    Directory.CreateDirectory(nsp);
                                    if (!Directory.Exists(nsp))
                                    {
                                        sp = GlobalMethord.ReplacePath(nsp);
                                        Directory.CreateDirectory(nsp);
                                    }
                                }
                                di = new DownloadItem
                                {
                                    FileName = fi.FileNames[index],
                                    Link = fi.ContentUrls[index],
                                    SavePath = nsp,
                                    SourceDocu = fi,
                                    AId = _tempAI
                                };
                                di.CheckTempFile();
                            }
                            GlobalData.VM_DL.DownLoadItemList.Add(di);
                        }
                        break;
                    default:
                        {
                            BaseItem bi = (BaseItem)args[1];
                            string sp = Path.Combine(_savePath, _tempAN, $"{bi.CreateDate.ToString("yyyy-MM-dd-HH")}_{bi.Title}");
                            try
                            {
                                Directory.CreateDirectory(sp);
                            }
                            catch
                            {
                                if (!Directory.Exists(sp))
                                {
                                    sp = GlobalMethord.RemoveLastDot(GlobalMethord.ReplacePath(sp));
                                    Directory.CreateDirectory(sp);
                                }
                            }
                            if (index == -1)
                            {
                                di = new DownloadItem
                                {
                                    FileName = bi.CoverPicName,
                                    Link = bi.CoverPic,
                                    SavePath = sp,
                                    CTime = bi.CreateDate,
                                    SourceDocu = bi,
                                    AId = _tempAI
                                };
                                di.CheckTempFile();
                            }
                            else
                            {
                                di = new DownloadItem
                                {
                                    FileName = bi.FileNames[index],
                                    Link = bi.ContentUrls[index],
                                    SavePath = sp,
                                    CTime = bi.CreateDate,
                                    SourceDocu = bi,
                                    AId = _tempAI
                                };
                                di.CheckTempFile();
                            }
                            GlobalData.VM_DL.DownLoadItemList.Add(di);
                        }
                        break;
                }
            });
        }

        public ParamCommand<FantiaItem> AddFantiaCommand
        {
            get => new ParamCommand<FantiaItem>((fi) =>
            {
                DownloadItem di = null;
                //foreach (FantiaItem fi in fis)
                {
                    string sp = Path.Combine(_savePath, GlobalData.VM_MA.Artist.AName, $"{fi.CreateDate.ToString("yyyy-MM-dd-HH")}_{fi.Title}");
                    try
                    {
                        Directory.CreateDirectory(sp);
                    }
                    catch
                    {
                        if (!Directory.Exists(sp))
                        {
                            sp = GlobalMethord.RemoveLastDot(GlobalMethord.ReplacePath(sp));
                            Directory.CreateDirectory(sp);
                        }
                    }
                    GlobalData.DLLogs.SetPId(fi.ID);
                    if (!string.IsNullOrEmpty(fi.CoverPic))
                    {
                        if (!GlobalData.DLLogs.HasLog(fi.CoverPic))
                        {
                            di = new DownloadItem
                            {
                                FileName = fi.CoverPicName,
                                Link = fi.CoverPic,
                                SavePath = sp,
                                SourceDocu = fi,
                                AId = _tempAI
                            };
                            di.CheckTempFile();
                            GlobalData.SyContext.Send((dd) =>
                            {
                                GlobalData.VM_DL.DownLoadItemList.Add((DownloadItem)dd);
                            }, di);
                        }
                    }
                    for (int i = 0; i < fi.ContentUrls.Count; i++)
                    {
                        if (GlobalMethord.OverPayment(int.Parse(fi.Fees[i])))
                        {
                            continue;
                        }
                        if (!GlobalData.DLLogs.HasLog(fi.ContentUrls[i]))
                        {
                            var nsp = $"{sp}\\{fi.CreateDate.ToString("yyyy-MM-dd-HH")}_{fi.Title}-{fi.PTitles[i]}";
                            if (!Directory.Exists(nsp))
                            {
                                Directory.CreateDirectory(nsp);
                                if (!Directory.Exists(nsp))
                                {
                                    sp = GlobalMethord.ReplacePath(nsp);
                                    Directory.CreateDirectory(nsp);
                                }
                            }
                            di = new DownloadItem
                            {
                                FileName = fi.FileNames[i],
                                Link = fi.ContentUrls[i],
                                SavePath = nsp,
                                SourceDocu = fi,
                                AId = _tempAI
                            };
                            di.CheckTempFile();
                            if (di.FileName.StartsWith("dimg:"))
                            {
                                di.FileName = di.FileName.Substring(5);
                                di.IsDataImage = true;
                            }
                            GlobalData.SyContext.Send((dd) =>
                            {
                                GlobalData.VM_DL.DownLoadItemList.Add((DownloadItem)dd);
                            }, di);
                        }
                    }
                    if (!_isDownloading && QuestCommand.CanExecute(null))
                    {
                        QuestCommand.Execute(null);
                    }
                    else
                    {
                        BeginNextCommand.Execute(null);
                    }
                    if (fi.Comments.Count > 0)
                    {
                        var fp = Path.Combine(sp, "Comment.html");
                        if (File.Exists(fp))
                        {
                            var cms = File.ReadAllLines(fp);
                            if (cms.Except(fi.Comments).Count() == 0)
                            {
                                return;
                            }
                        }
                        File.WriteAllLines(Path.Combine(sp, "Comment.txt"), fi.Comments);
                    }
                }
            });
        }

        public ParamCommand<DownloadItem> AddRetryCommand
        {
            get => new ParamCommand<DownloadItem>((di) =>
            {
                if (_isFantia)
                {
                    lock (lock_Fantia)
                    {
                        _retryList.Add(di.SourceDocu.ID);
                    }
                }

            });
        }

        public CommonCommand FantiaRetryCommand
        {
            get => new CommonCommand(async () =>
            {
                string[] tar = _retryList.ToArray();
                _retryList.Clear();
                var dler = _downLoadList.ToList();
                foreach (var fi_old in tar)
                {
                    var fi_new = await GlobalData.FantiaRetryUtil.GetUrls(fi_old);
                    GlobalData.DLLogs.SetPId(fi_new.ID);
                    if (!string.IsNullOrEmpty(fi_new.CoverPic))
                    {
                        if (!GlobalData.DLLogs.HasLog(fi_new.CoverPic))
                        {
                            var keys = fi_new.CoverPic.Split('?').FirstOrDefault();
                            if (GlobalData.RetryCounter.ContainsKey(keys))
                            {
                                GlobalData.RetryCounter[keys]++;
                            }
                            else
                            {
                                GlobalData.RetryCounter.Add(keys, 1);
                            }
                            var dtar = dler.Find(x => x.Link.Contains(keys));
                            dtar.Link = fi_new.CoverPic;
                            dtar.SourceDocu = fi_new;
                            dtar.CheckTempFile();
                            if (GlobalData.RetryCounter[keys] < 15)
                            {
                                dtar.DLStatus = DownloadStatus.Waiting;
                            }
                            else
                            {
                                dtar.ErrorMsg = "HTTP403";
                                dtar.DLStatus = DownloadStatus.Cancel;
                            }
                        }
                    }
                    for (int i = 0; i < fi_new.ContentUrls.Count; i++)
                    {
                        if (GlobalMethord.OverPayment(int.Parse(fi_new.Fees[i])))
                        {
                            continue;
                        }
                        if (!GlobalData.DLLogs.HasLog(fi_new.ContentUrls[i]))
                        {
                            var keys = fi_new.ContentUrls[i].Split('?').FirstOrDefault();
                            if (GlobalData.RetryCounter.ContainsKey(keys))
                            {
                                GlobalData.RetryCounter[keys]++;
                            }
                            else
                            {
                                GlobalData.RetryCounter.Add(keys, 1);
                            }
                            var dtar = dler.Find(x => x.Link.Contains(keys));
                            dtar.Link = fi_new.ContentUrls[i];
                            dtar.SourceDocu = fi_new;
                            dtar.CheckTempFile();
                            if (GlobalData.RetryCounter[keys] < 15)
                            {
                                dtar.DLStatus = DownloadStatus.Waiting;
                            }
                            else
                            {
                                dtar.ErrorMsg = "HTTP403";
                                dtar.DLStatus = DownloadStatus.Cancel;
                            }
                        }
                    }
                    if (!_isDownloading && QuestCommand.CanExecute(null))
                    {
                        QuestCommand.Execute(null);
                    }
                    else
                    {
                        BeginNextCommand.Execute(null);
                    }
                }
            });
        }

        public ParamCommand<DownloadItem> BeginNextCommand
        {
            get => new ParamCommand<DownloadItem>((di) =>
            {
                if (_isDownloading)
                {
                    lock (lock_Dl)
                    {
                        if (null != di)
                        {
                            _dlClients.RemoveAll(x => x.Equals(di));
                        }
                        int cou = 0;
                        for (int i = 0; i < _downLoadList.Count; i++)
                        {
                            if (_downLoadList[i].DLStatus == DownloadStatus.Waiting)
                            {
                                if (_dlClients.Count < _threadCount)
                                {
                                    if (!_dlClients.Contains(_downLoadList[i]))
                                    {
                                        _dlClients.Add(_downLoadList[i]);
                                    }
                                    _downLoadList[i].Start();
                                }
                                else
                                {
                                    return;
                                }
                            }
                            else if (_downLoadList[i].DLStatus == DownloadStatus.Downloading || _downLoadList[i].DLStatus == DownloadStatus.WriteFile)
                            {
                                cou++;
                            }
                        }
                        if (cou > 0)
                        {
                            return;
                        }
                    }

                    IsDownloading = false;
                    for (int i = 0; i < _downLoadList.Count; i++)
                    {
                        if (_downLoadList[i].DLStatus != DownloadStatus.Error)
                        {
                            return;
                        }
                        else if (!_downLoadList[i].ErrorMsg.Contains("HTTP404") && !_downLoadList[i].ErrorMsg.Contains("HTTP500"))
                        {
                            if (_isFantia && _retryList.Count != 0)
                            {
                                FantiaRetryCommand.Execute(null);
                            }
                            return;
                        }
                    }
                    if (!_isFantia)
                    {
                        if (string.IsNullOrEmpty(GlobalData.VM_MA.Date_End))
                        {
                            GlobalData.VM_MA.Date_Start = GlobalData.StartTime.ToString("yyyy/MM/dd HH:mm:ss");
                            GlobalData.LastDateDic.Update(GlobalData.VM_MA.LastDate);
                        }
                        else
                        {
                            GlobalData.VM_MA.LastDate = GlobalData.VM_MA.LastDate_End;
                            GlobalData.LastDateDic.Update(GlobalData.VM_MA.LastDate_End);
                        }
                    }
                    else
                    {
                        if (_retryList.Count != 0)
                        {
                            FantiaRetryCommand.Execute(null);
                        }
                        else
                        {
                            if (GlobalData.VM_MA.ShowLoad)
                            {
                                return;
                            }
                            if (string.IsNullOrEmpty(GlobalData.VM_MA.Date_End))
                            {
                                GlobalData.VM_MA.Date_Start = GlobalData.StartTime.ToString("yyyy/MM/dd HH:mm:ss");
                                GlobalData.LastDateDic.Update(GlobalData.VM_MA.LastDate);
                            }
                            else
                            {
                                GlobalData.VM_MA.LastDate = GlobalData.VM_MA.LastDate_End;
                                GlobalData.LastDateDic.Update(GlobalData.VM_MA.LastDate_End);
                            }
                        }
                    }
                }
            });
        }

        public ParamCommand<DownloadItem> OpenFileCommand
        {
            get => new ParamCommand<DownloadItem>((di) =>
            {
                System.Diagnostics.Process.Start($"{di.SavePath}\\{di.FileName}");
            });
        }

        public ParamCommand<DownloadItem> OpenFolderCommand
        {
            get => new ParamCommand<DownloadItem>((di) =>
            {
                GlobalMethord.ExplorerFile($"{di.SavePath}\\{di.FileName}");
            });
        }

        public ParamCommand<DownloadItem> OpenDocumentCommand
        {
            get => new ParamCommand<DownloadItem>((di) =>
            {
                GlobalCommand.ShowDocumentCommand.Execute(di.SourceDocu);
                System.Windows.Application.Current.MainWindow.Activate();
            });
        }

        public ParamCommand<DownloadItem> ToFirstCommand
        {
            get => new ParamCommand<DownloadItem>((di) =>
            {
                lock (lock_DList)
                {
                    _downLoadList.Remove(di);
                    _downLoadList.Insert(0, di);
                }
            });
        }

        public ParamCommand<DownloadItem> StartCommand
        {
            get => new ParamCommand<DownloadItem>((di) =>
            {
                if (_dlClients.Count < _threadCount)
                {
                    lock (lock_Dl)
                    {
                        if (_dlClients.Count < _threadCount)
                        {
                            if (!_dlClients.Contains(di))
                            {
                                _dlClients.Add(di);
                            }
                            di.ReTryCount = 0;
                            di.Start();
                            if (_dlClients.Count == _threadCount || _dlClients.Count == _downLoadList.Count)
                            {
                                IsDownloading = true;
                            }
                        }
                    }
                }
                else
                {
                    di.DLStatus = DownloadStatus.Waiting;
                }
            });
        }

        public ParamCommand<DownloadItem> PauseCommand
        {
            get => new ParamCommand<DownloadItem>(async (di) =>
            {
                await di.Pause();
                BeginNextCommand.Execute(di);
            });
        }

        public ParamCommand<DownloadItem> ReStartCommand
        {
            get => new ParamCommand<DownloadItem>(async (di) =>
            {
                if (di.DLStatus == DownloadStatus.Downloading)
                {
                    if (await di.Pause())
                    {
                        di.Start();
                    }
                }
                else if (di.DLStatus != DownloadStatus.WriteFile)
                {
                    lock (lock_DList)
                    {
                        di.DLStatus = DownloadStatus.Waiting;
                        BeginNextCommand.Execute(null);
                        //_downLoadList.Remove(di);
                        //_downLoadList.Insert(0, di);
                    }
                }
            });
        }

        public ParamCommand<DownloadItem> CancelCommand
        {
            get => new ParamCommand<DownloadItem>(async (di) =>
            {
                if (di.DLStatus == DownloadStatus.Cancel)
                    return;
                
                if (di.DLStatus == DownloadStatus.Downloading)
                {
                    await di.Cancel();
                    BeginNextCommand.Execute(di);
                }
                else
                {
                    di.DLStatus = DownloadStatus.Cancel;
                }
                lock (lock_DList)
                {
                    _downLoadList.Remove(di);
                    _downLoadList.Add(di);
                }
            });
        }

    }
}
