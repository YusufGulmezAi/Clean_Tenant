using Microsoft.Extensions.Localization;
using MudBlazor;

namespace CleanTenant.ManagementApp.Services;

/// <summary>
/// <para>
/// MudBlazor'ın default İngilizce resource'larını Türkçe karşılıklarıyla
/// override eder. <see cref="MudDataGrid{T}"/> filtre/gruplama/sıralama/loading
/// mesajları + ortak buton metinleri (Apply/Cancel/Clear/Save/Reset) Türkçeleştirildi.
/// </para>
/// <para>
/// Eşleştirilmemiş key'ler için <c>resourceNotFound: true</c> dönülür — MudBlazor
/// kendi fallback'ine düşer (default İngilizce). Bu sayede yeni MudBlazor sürümü
/// yeni key'ler eklediğinde uygulama kırılmaz, sadece o key'ler İngilizce kalır.
/// </para>
/// </summary>
public sealed class CleanTenantMudLocalizer : MudLocalizer
{
    /// <inheritdoc />
    public override LocalizedString this[string key] => key switch
    {
        // ─── MudDataGrid — Filter / Sort / Group / Loading ────────────────
        "MudDataGrid.AddFilter" => Tr(key, "Filtre Ekle"),
        "MudDataGrid.Apply" => Tr(key, "Uygula"),
        "MudDataGrid.CancelButton" => Tr(key, "İptal"),
        "MudDataGrid.Cancel" => Tr(key, "İptal"),
        "MudDataGrid.Clear" => Tr(key, "Temizle"),
        "MudDataGrid.ClearFilter" => Tr(key, "Filtreyi Temizle"),
        "MudDataGrid.CollapseAllGroups" => Tr(key, "Tüm Grupları Daralt"),
        "MudDataGrid.Columns" => Tr(key, "Sütunlar"),
        "MudDataGrid.Contains" => Tr(key, "İçerir"),
        "MudDataGrid.DoesNotContain" => Tr(key, "İçermez"),
        "MudDataGrid.DragHeaderHere" => Tr(key, "Gruplamak için başlığı buraya sürükleyin"),
        "MudDataGrid.EndsWith" => Tr(key, "İle Biter"),
        "MudDataGrid.Equal" => Tr(key, "Eşittir"),
        "MudDataGrid.Equals" => Tr(key, "Eşittir"),
        "MudDataGrid.ExpandAllGroups" => Tr(key, "Tüm Grupları Genişlet"),
        "MudDataGrid.False" => Tr(key, "Hayır"),
        "MudDataGrid.Filter" => Tr(key, "Filtrele"),
        "MudDataGrid.FilterValue" => Tr(key, "Filtre Değeri"),
        "MudDataGrid.GreaterThan" => Tr(key, "Büyüktür"),
        "MudDataGrid.GreaterThanOrEqual" => Tr(key, "Büyük Eşit"),
        "MudDataGrid.Group" => Tr(key, "Grupla"),
        "MudDataGrid.Hide" => Tr(key, "Gizle"),
        "MudDataGrid.HideAll" => Tr(key, "Tümünü Gizle"),
        "MudDataGrid.HideAllGroupings" => Tr(key, "Tüm Gruplamaları Gizle"),
        "MudDataGrid.IsEmpty" => Tr(key, "Boş"),
        "MudDataGrid.IsNotEmpty" => Tr(key, "Boş Değil"),
        "MudDataGrid.LessThan" => Tr(key, "Küçüktür"),
        "MudDataGrid.LessThanOrEqual" => Tr(key, "Küçük Eşit"),
        "MudDataGrid.Loading" => Tr(key, "Yükleniyor…"),
        "MudDataGrid.NoRecords" => Tr(key, "Kayıt bulunamadı"),
        "MudDataGrid.NoRecordsContent" => Tr(key, "Kayıt bulunamadı"),
        "MudDataGrid.NotEqual" => Tr(key, "Eşit Değil"),
        "MudDataGrid.Operator" => Tr(key, "Operatör"),
        "MudDataGrid.RefreshData" => Tr(key, "Verileri Yenile"),
        "MudDataGrid.Save" => Tr(key, "Kaydet"),
        "MudDataGrid.Show" => Tr(key, "Göster"),
        "MudDataGrid.ShowAll" => Tr(key, "Tümünü Göster"),
        "MudDataGrid.ShowAllGroupings" => Tr(key, "Tüm Gruplamaları Göster"),
        "MudDataGrid.Sort" => Tr(key, "Sırala"),
        "MudDataGrid.SortAscending" => Tr(key, "Artan Sırala"),
        "MudDataGrid.SortDescending" => Tr(key, "Azalan Sırala"),
        "MudDataGrid.StartsWith" => Tr(key, "İle Başlar"),
        "MudDataGrid.True" => Tr(key, "Evet"),
        "MudDataGrid.Ungroup" => Tr(key, "Grubu Kaldır"),
        "MudDataGrid.Unsort" => Tr(key, "Sıralamayı Kaldır"),
        "MudDataGrid.Value" => Tr(key, "Değer"),

        // ─── Aggregate / Footer ────────────────────────────────────
        "MudDataGrid.Sum" => Tr(key, "Toplam"),
        "MudDataGrid.Average" => Tr(key, "Ortalama"),
        "MudDataGrid.Min" => Tr(key, "En Küçük"),
        "MudDataGrid.Max" => Tr(key, "En Büyük"),
        "MudDataGrid.Count" => Tr(key, "Adet"),
        "MudDataGrid.None" => Tr(key, "Yok"),
        "MudDataGrid.Custom" => Tr(key, "Özel"),

        // ─── MudTable / Pagination ─────────────────────────────────
        "MudTable.Loading" => Tr(key, "Yükleniyor…"),
        "MudTable.NoRecords" => Tr(key, "Kayıt bulunamadı"),
        "MudDataGrid.RowsPerPage" => Tr(key, "Sayfa başına kayıt:"),
        "MudTable.RowsPerPage" => Tr(key, "Sayfa başına kayıt:"),
        "MudDataGrid.PageInfo" => Tr(key, "{0}-{1} / {2}"),
        "MudTable.PageInfo" => Tr(key, "{0}-{1} / {2}"),
        "MudDataGrid.FirstPage" => Tr(key, "İlk sayfa"),
        "MudDataGrid.LastPage" => Tr(key, "Son sayfa"),
        "MudDataGrid.PreviousPage" => Tr(key, "Önceki sayfa"),
        "MudDataGrid.NextPage" => Tr(key, "Sonraki sayfa"),

        // ─── MudInput / MudSelect / MudFileUpload ─────────────────
        "MudInput.OK" => Tr(key, "Tamam"),
        "MudInput.Cancel" => Tr(key, "İptal"),
        "MudInput.Clear" => Tr(key, "Temizle"),
        "MudSelect.Clear" => Tr(key, "Temizle"),
        "MudFileUpload.DefaultDragAndDropTitle" => Tr(key, "Dosyaları buraya sürükleyip bırakın veya gözat"),

        // ─── MudDateRangePicker / MudDatePicker ───────────────────
        "MudDateRangePicker.StartDate" => Tr(key, "Başlangıç tarihi"),
        "MudDateRangePicker.EndDate" => Tr(key, "Bitiş tarihi"),
        "MudDatePicker.Today" => Tr(key, "Bugün"),
        "MudDatePicker.Clear" => Tr(key, "Temizle"),

        // ─── MudDialog ────────────────────────────────────────────
        "MudDialog.Ok" => Tr(key, "Tamam"),
        "MudDialog.Cancel" => Tr(key, "İptal"),
        "MudDialog.Yes" => Tr(key, "Evet"),
        "MudDialog.No" => Tr(key, "Hayır"),

        // ─── MudMessageBox ────────────────────────────────────────
        "MudMessageBox.OK" => Tr(key, "Tamam"),
        "MudMessageBox.Cancel" => Tr(key, "İptal"),
        "MudMessageBox.Yes" => Tr(key, "Evet"),
        "MudMessageBox.No" => Tr(key, "Hayır"),

        // Eşleştirme yok → resourceNotFound:true → MudBlazor default'a düşer
        _ => new LocalizedString(key, key, resourceNotFound: true),
    };

    private static LocalizedString Tr(string key, string value)
        => new(key, value, resourceNotFound: false);
}
