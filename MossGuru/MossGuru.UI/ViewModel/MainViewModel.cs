using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using MossGuru.Core;
using MossGuru.Core.Util;
using MossGuru.UI.Properties;
using MossGuru.UI.Services;
using MossGuru.UI.Validation;

namespace MossGuru.UI.ViewModel
{
  public class MainViewModel : ValidationViewModelBase
  {
    private bool isBusy;
    private bool hasValidationError;
    private string userId;
    private ObservableCollection<string> availableLanguages;
    private string selectedLanguage;
    private int maxMatches;
    private int displayResults;
    private string extensionsString;
    private string ignoreFoldersString;
    private string selectedFolder;
    private ObservableCollection<string> studentFolders;
    private Uri resultUri;
    private string requestErrorMsg;
    private string clientState;

    public MainViewModel()
    {
      this.AvailableLanguages = new ObservableCollection<string>(Settings.Default.mossLanguages.Split(','));
      this.SelectedLanguage = this.AvailableLanguages.FirstOrDefault();
      this.MaxMatches = 3;
      this.DisplayResults = 250;
      this.OpenFolderCommand = new RelayCommand(() =>
      {
        var selectedFolder = new DialogService().OpenFolderDialog();
        if (!string.IsNullOrEmpty(selectedFolder))
        {
          this.SelectedFolder = selectedFolder;
        }
      });

      this.ScanFolderCommand = new RelayCommand(this.ScanFolder);
      this.SubmitCommand = new RelayCommand(this.Submit, () => !this.HasValidationError && !string.IsNullOrEmpty(this.SelectedFolder));
    }

    public bool IsBusy
    {
      get { return this.isBusy; }
      set { this.Set(ref isBusy, value); }
    }

    public bool HasValidationError
    {
      get { return this.hasValidationError; }
      set { this.Set(ref hasValidationError, value); }
    }

    [Required]
    [MinLength(9, ErrorMessage = "Minimum Length 9")]
    public string UserId
    {
      get { return this.userId; }
      set { Set(ref userId, value); }
    }

    public ObservableCollection<string> AvailableLanguages
    {
      get { return this.availableLanguages; }
      set { this.Set(ref availableLanguages, value); }
    }

    [Required]
    public string SelectedLanguage
    {
      get { return this.selectedLanguage; }
      set { this.Set(ref selectedLanguage, value); }
    }

    [Required]
    [Range(1, 100, ErrorMessage = "MaxMatches Range 1-100")]
    public int MaxMatches
    {
      get { return this.maxMatches; }
      set { this.Set(ref maxMatches, value); }
    }

    [Required]
    [Range(1, 1000, ErrorMessage = "Display Results Range 1 - 1000")]
    public int DisplayResults
    {
      get { return this.displayResults; }
      set { this.Set(ref displayResults, value); }
    }

    [Required]
    [ExtensionValidation(ErrorMessage = "Format: [ext1, ext2, ext3] wihout preceding dots")]
    public string ExtensionsString
    {
      get { return this.extensionsString; }
      set { this.Set(ref extensionsString, value); }
    }

    // TODO: Validator
    [ExtensionValidation(ErrorMessage = "Format: [bin, obj] wihout slashes")]
    public string IgnoreFoldersString
    {
      get { return this.ignoreFoldersString; }
      set { this.Set(ref ignoreFoldersString, value); }
    }

    [Required]
    public string SelectedFolder
    {
      get { return this.selectedFolder; }
      set { this.Set(ref selectedFolder, value); }
    }

    public ObservableCollection<string> StudentFolders
    {
      get { return this.studentFolders; }
      set { this.Set(ref studentFolders, value); }
    }

    public Uri ResultUri
    {
      get { return this.resultUri; }
      set { this.Set(ref resultUri, value); }
    }

    public string RequestErrorMsg
    {
      get { return this.requestErrorMsg; }
      set { this.Set(ref requestErrorMsg, value); }
    }


    public string ClientState
    {
      get { return this.clientState; }
      set { this.Set(ref clientState, value); }
    }


    public RelayCommand OpenFolderCommand { get; set; }
    public RelayCommand ScanFolderCommand { get; set; }
    public RelayCommand SubmitCommand { get; set; }

    private void ScanFolder()
    {
      Task.Run(() =>
      {
        this.IsBusy = true;
        try
        {
          var studentDirs = FileUtils.GetTopLevelDirectories(this.SelectedFolder).Select(f => f.Name);
          this.StudentFolders = new ObservableCollection<string>(studentDirs);
        }
        finally
        {
          this.IsBusy = false;
        }
      });
    }

    private void Submit()
    {
      Task.Run(() =>
      {
        try
        {
          this.IsBusy = true;
          var client = new MossClient("moss.stanford.edu", 7690, this.UserId, this.SelectedLanguage, this.MaxMatches,
            this.DisplayResults, "Sent by MossGuru");
          var baseDir = new DirectoryInfo(this.SelectedFolder);
          var studentDirs = FileUtils.GetTopLevelDirectories(baseDir.FullName);
          client.MossStatusUpdate += (s, c) =>
          {
            this.ClientState = s;
          };

          var extArray = Regex.Replace(this.ExtensionsString, @"\s+", "").Split(',');
          extArray = extArray.Select(s => s[0].Equals('.') ? s : s.Insert(0, ".")).ToArray();
          string[] ignoreArray = null;
          if(!string.IsNullOrEmpty(this.IgnoreFoldersString))
            ignoreArray = Regex.Replace(this.IgnoreFoldersString, @"\s+", "").Split(',');

          var result = client.SendFiles(baseDir, studentDirs, extArray, ignoreArray);

          if (result.Success)
            this.ResultUri = result.ResultUri;
          else
            this.RequestErrorMsg = result.Status;
        }
        catch (Exception ex)
        {
          this.RequestErrorMsg = ex.Message;
        }
        finally
        {
          this.ClientState = string.Empty;
          this.IsBusy = false;
        }
      });
    }
  }
}