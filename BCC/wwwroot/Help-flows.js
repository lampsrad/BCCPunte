/* Code-flow descriptions for Help.html -> keyed by data-flow-id */
(function () {
  function esc(s) {
    return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
  }
  function fmtStep(s) {
    if (/^---/.test(s)) return '<li class="flow-branch">' + esc(s) + '</li>';
    var sep = ' -> ', i = s.indexOf(sep);
    if (i >= 0) return '<li><code>' + esc(s.slice(0, i)) + '</code>' + sep + esc(s.slice(i + sep.length)) + '</li>';
    return '<li><code>' + esc(s) + '</code></li>';
  }
  function flow(steps) {
    return '<ol class="flow-steps">' + steps.map(fmtStep).join('') + '</ol>';
  }

  var DATA = {
    "db-key": [
      "Menu.razor -> render @gData.connectionKey",
      "DbContextFactory -> select connection string for that key",
      "All later Repo / EF calls use the selected connection"
    ],
    "month-title": [
      "Menu.OnAfterRender (first) -> state.Menu += stateChanged",
      "state.Title = state.DatePhoto.toMonthFull_Year()",
      "StateHasChanged()",
      "--- on later date change ---",
      "stateChanged() -> update state.Title -> InvokeAsync(StateHasChanged)"
    ],
    "bcc": [
      "Menu.razor -> <a href=\"/\">BCC</a>",
      "Blazor navigate -> /",
      "Index.OnInitializedAsync",
      "repo.lastDateAsync -> lastImport + clubYearStart",
      "state.Date / DatePhoto = last import",
      "LoadWinnersAsync -> scan photosLocal for Temp folders",
      "repo.GetEntitiesNTAsync<Photo>(Winner || Club_Winner)",
      "Order by Star_Group, Category -> render winner cards"
    ],
    "awards": [
      "Menu.razor -> <a href=\"results/\">Awards</a>",
      "Navigate -> /results",
      "Results.razor + DateChange",
      "On month change -> set state.Date / DatePhoto",
      "Load Photos for state.DatePhoto",
      "Build ResultsVM (N1–N4, P1–P4, S, PH) + Winners",
      ".toPhotosView() enrich names / filenames",
      "Render tables"
    ],
    "scores": [
      "Menu.razor -> <a href=\"points/\">Scores</a>",
      "Navigate -> /points",
      "Points.razor + DateChange",
      "Load monthlies for club-year window",
      "Build top-10 Py / VOy / Saly",
      "Build promotions list (current month)",
      "Render StarLevelTable S1–S7",
      "--- All button on a row ---",
      "Navigate -> /pointsmember/{MasterID}"
    ],
    "salons-view": [
      "Menu.razor -> <a href=\"salons/\">Salons</a>",
      "Navigate -> /salons",
      "Salons.razor + DateChange IsSalons=\"true\"",
      "Month / PSY / BCY / NEW -> callback scope",
      "Load Salon rows + SalonMaster + Master",
      "Render participation table"
    ],
    "interclub": [
      "Menu.razor -> @onclick=\"InterClub\"",
      "Menu.InterClub()",
      "cutoffDate = Today.AddMonths(-12)",
      "repo.GetEntitiesNTAsync<Photo>(Date > cutoff && Score > 10)",
      "--- Juniors: Club_Rating < 4 ---",
      "SetPhotoFilename -> match IntRef under photosLocal\\yyyy-MM",
      "ExportInterClubPhoto -> gData.Exports\\Juniors\\",
      "--- Seniors: Club_Rating > 3, Score > 11 (exclude Firstname Trix) ---",
      "ExportInterClubPhoto -> Exports\\Seniors\\",
      "--- Seniors_11: Score == 11 ---",
      "ExportInterClubPhoto -> Exports\\Seniors_11\\",
      "File.Copy with sanitised filename"
    ],
    "members-list": [
      "Menu.razor -> <a href=\"masterview\">Members List</a>",
      "Navigate -> /masterview",
      "MasterView.OnInitializedAsync",
      "repo.GetEntitiesNTAsync<Master>(null, order Name)",
      "--- Update ---",
      "Directory.GetFiles(Downloads, \"*Ledelys*.csv\")",
      "All masters Paid=false -> parse CSV -> update/add -> message UPDATED",
      "--- List ---",
      "MemberList() -> write Members.csv for Paid masters"
    ],
    "honours": [
      "Menu.razor -> <a href=\"honours\" title=\"Adjust Honours\">Honours</a>",
      "Navigate -> /honours",
      "Honours.OnInitializedAsync",
      "DataService.Masters() -> masters with IdVault",
      "OnAfterRender -> refmember.FocusAsync()",
      "--- Member select ---",
      "OnIDChanged -> DataService.Master(ID) -> mod.PrevTitle = mas.Title",
      "--- Submit ---",
      "Submitted() -> mod.Title = ToUpper()",
      "repo.GetEntityNTAsync<Master> + monthlyLastNTAsync",
      "mas.Title = $\"{mas.Title} {mod.Title}\" (append)",
      "If APSSA/FPSSA -> mon.Title = mod.Title",
      "Evaluate mon.Promotion (5★/APSSA or 6★/FPSSA thresholds)",
      "If true -> RatingID++, PromoString, clear GMp/Pp/Mg/Gg/Sg/Bg",
      "UpdateSaveDetachAsync(mas) + UpdateSaveDetachAsync(mon)",
      "NavigateTo(\"/\")",
      "--- Cancel ---",
      "Abort() -> NavigateTo(\"/\") (no DB write)"
    ],
    "salons-current": [
      "Menu.razor -> <a href=\"salonMasMain\">Salons Current</a>",
      "Navigate -> /salonMasMain",
      "SalonMasMain.OnParametersSetAsync (Data null = current)",
      "Load SalonMaster list for current window",
      "Render SalonMasIndex",
      "--- click salon name ---",
      "SalonMasIndex.SalonImport(sm)",
      "If already has Salons -> message \"Already Imported\"",
      "state.ShowFileUpload -> Import folder",
      "SalonImport.ImportSalon(sm, salonname)"
    ],
    "salons-previous": [
      "Menu.razor -> <a href=\"salonMasMain/prev\">Salons Previous</a>",
      "Navigate -> /salonMasMain/prev",
      "SalonMasMain.OnParametersSetAsync Data=\"prev\"",
      "Load previous-period SalonMaster list",
      "Render SalonMasIndex (same actions as Current)"
    ],
    "import-salons": [
      "Menu.razor -> <a href=\"salonMasMain/importlist\">Import Salons</a>",
      "Navigate -> /salonMasMain/importlist",
      "SalonMasMain.OnParametersSetAsync Data=\"importlist\"",
      "ImportSalonList()",
      "state.ShowFilePicker() -> Downloads CSV",
      "TextFieldParser -> Club, SalonName, Date (dd-MM-yyyy)",
      "Bulk add SalonMaster entities"
    ],
    "info": [
      "Menu.OnInitialized",
      "Directory.EnumerateFiles(WebRootPath\\Html\\, \"*.html\")",
      "menuItems.Add(Path.GetFileNameWithoutExtension(file))",
      "Menu.razor foreach -> <a href=\"info/{nam}\">{nam}</a>",
      "Navigate -> /info/{nam}",
      "Info.razor load wwwroot/Html/{nam}.html -> display"
    ],
    "images-process": [
      "Menu.razor -> <a href=\"admin/images-import\">Images Process</a>",
      "Navigate -> /admin/images-import",
      "Admin.OnParametersSetAsync Data=\"images-import\"",
      "ImagesProcessAsync()",
      "PhotosLocalUnzipAsync -> file upload + extract *.zip -> photosLocal\\{yyyy-MM}\\",
      "PhotosCopyRenameAsync -> Temp\\#######.jpg",
      "PhotosGeneratePreviewsAsync(1365, 768) -> WEB\\",
      "PhotosZipAsync -> ZippedExport + ClubPhotos copy + clear WEB",
      "UploadToHosting -> JWT Auth/login + POST FU/zip"
    ],
    "import-club": [
      "Menu.razor -> <a href=\"admin/club-import\">Import Club</a>",
      "Navigate -> /admin/club-import",
      "Admin.OnParametersSetAsync Data=\"club-import\"",
      "ImportClub()",
      "ds.LastDates()",
      "--- October / year-end branch if month == 9 ---",
      "Scores CSV -> HeadersCheckScores -> processLineScores",
      "GetMasterRow / AddNewMaster (confirm dialog if new)",
      "Photo quantity check -> chooser if over limits",
      "Results CSV -> processLineResults",
      "MonthlyCompute + MonthlyUpdate",
      "DatesStoreInDb()",
      "ds.Promotion_Due()"
    ],
    "import-salon": [
      "Menu.razor -> <a href=\"salonMasMain\">Import Salon</a>",
      "Navigate -> /salonMasMain",
      "SalonMasMain load index",
      "User clicks salon name -> SalonMasIndex.SalonImport(sm)",
      "ShowFileUpload + SalonImport.ImportSalon",
      "Message with import results"
    ],
    "word": [
      "Menu.razor -> <a href=\"worddoc\">Word</a>",
      "Navigate -> /worddoc",
      "WordDoc InputFile -> OnFileSelected",
      "Validate .docx",
      "WordprocessingDocument.Open + HtmlConverter.ConvertToHtml",
      "File.WriteAllText(wwwroot/Html/{name}.html)",
      "NavigateTo($\"info/{destfile}\")"
    ],
    "upload-abs": [
      "Menu.razor -> <a href=\"/upload-to-abs\">Upload to AbsHosting</a>",
      "Navigate -> /upload-to-abs",
      "FileUploadAbs.HandleFileSelected",
      "HTTP POST to Absolute Hosting API",
      "Status message on page"
    ],
    "backup": [
      "Menu.razor -> <a href=\"admin/backup\">Backup DB</a>",
      "Navigate -> /admin/backup",
      "Admin.OnParametersSetAsync Data=\"backup\"",
      "Backup() -> filePick=true, Title=Backup",
      "Backup.razor list *.bak + text input",
      "Submit -> ecbBackup(file)",
      "repo.SqlBackupAsync(file)",
      "Messages.Add success / Errors on failure"
    ],
    "restore": [
      "Menu.razor -> <a href=\"admin/restore\">Restore DB</a>",
      "Navigate -> /admin/restore",
      "Admin.OnParametersSetAsync Data=\"restore\"",
      "Restore() -> move Downloads\\bcc.bak if present",
      "filePick=true, Title=Restore",
      "Submit -> ecbRestore(file)",
      "repo.SqlRestoreAsync(file)"
    ],
    "excel": [
      "Menu.razor -> <a href=\"admin/excel\">Excel</a>",
      "Navigate -> /admin/excel",
      "Admin.OnParametersSetAsync Data=\"excel\"",
      "Excel() datstart=2013-11-01 -> now",
      "state.ShowProgress(\"EXCEL\")",
      "For each month: latest Monthly per master -> CsvWrite",
      "state.Hide()"
    ],
    "delete-month": [
      "Menu.razor -> <a href=\"admin/deletemonth\">Delete Month</a>",
      "Navigate -> /admin/deletemonth",
      "Admin.OnParametersSetAsync Data=\"deletemonth\"",
      "DeleteMonth()",
      "BeginTransactionAsync",
      "Delete Photos then Monthlies for last import window",
      "Delete Salons then SalonMasters",
      "DatesStoreInDb()",
      "Restore Master.RatingID from remaining monthlies",
      "CommitAsync (or Rollback on error)"
    ],
    "help": [
      "Menu.razor -> <a href=\"/Help.html\" target=\"_blank\">Help</a>",
      "Browser opens new tab",
      "Static file served from wwwroot/Help.html"
    ],
    "help-merge": [
      "Menu.razor Admin → Help hover -> flyout Help_Merge (or Help.html toolbar)",
      "Menu.Help_Merge() / POST /api/help-flows/merge",
      "HelpMergeService.MergeAsync()",
      "Read Help-flows.json (effects, content, flows)",
      "Fill missing flows from Help-flows.js if needed",
      "Backup Help.html -> Help.html.bak",
      "HtmlAgilityPack: write effect-cell + data-edit-id content into Help.html",
      "SyncFlowsJsAsync -> rewrite var DATA in Help-flows.js (+ .bak)",
      "Clear Help-flows.json (keep empty flows/effects/content)",
      "ShowMessageAsync with merge counts"
    ],
    "db-local": [
      "Menu.razor -> @onclick=\"DatabaseLocal\"",
      "Menu.DatabaseLocal()",
      "gData.connectionKey = \"Local\"",
      "repo.GetEntityNTAsync<HitCounter>(ID==1)",
      "hc.Counter = gData.HitCount (if > 0)",
      "repo.UpdateSaveDetachAsync(hc)"
    ],
    "db-abshost": [
      "Menu.razor -> @onclick=\"DatabaseAbsolute\"",
      "Menu.DatabaseAbsolute()",
      "gData.connectionKey = \"Abshost\"",
      "repo.GetEntityNTAsync<HitCounter>(ID==1)",
      "gData.HitCount = hc.Counter ?? 0"
    ],
    "close": [
      "Menu.razor -> @if (env.IsProduction()) -> @onclick=\"Close\"",
      "Menu.Close()",
      "gData.process.CloseMainWindow()",
      "gData.process.Close()",
      "IHostApplicationLifetime.StopApplication()"
    ]
  };

  window.HELP_FLOWS = {};
  window.HELP_FLOWS_DATA = DATA;
  for (var id in DATA) {
    if (Object.prototype.hasOwnProperty.call(DATA, id)) {
      window.HELP_FLOWS[id] = flow(DATA[id]);
    }
  }
})();
