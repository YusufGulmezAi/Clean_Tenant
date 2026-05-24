namespace CleanTenant.Domain.Budgeting;

/// <summary>Bütçe şablonu görünürlüğü.</summary>
public enum TemplateVisibility
{
    /// <summary>Yalnız sahibi tenant görür/kullanır.</summary>
    Private = 0,

    /// <summary>Tüm tenant'lar görür ve kendi sitelerinde kullanabilir.</summary>
    Public = 1,
}
