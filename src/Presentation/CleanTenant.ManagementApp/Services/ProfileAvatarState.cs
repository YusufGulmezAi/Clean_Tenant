using CleanTenant.Application.Features.Profile.GetPhoto;
using MediatR;

namespace CleanTenant.ManagementApp.Services;

/// <summary>
/// <para>
/// Aktif kullanıcının profil fotoğrafını circuit boyunca paylaşan durum servisi
/// (Blazor Server'da Scoped = circuit başına tek örnek). AppBar avatar'ı ve
/// profil sayfası bu servisi kullanır; profil sayfasında foto yüklenip/silinince
/// <see cref="Changed"/> tetiklenir ve AppBar anında güncellenir (v0.2.13).
/// </para>
/// </summary>
public sealed class ProfileAvatarState
{
    private readonly IMediator _mediator;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public ProfileAvatarState(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Geçerli avatar'ın <c>data:</c> URI'si; foto yoksa <c>null</c>.</summary>
    public string? PhotoDataUri { get; private set; }

    /// <summary>İlk yükleme yapıldı mı (gereksiz tekrar sorguyu önler).</summary>
    public bool Loaded { get; private set; }

    /// <summary>Avatar değiştiğinde tetiklenir (AppBar abone olur).</summary>
    public event Action? Changed;

    /// <summary>Henüz yüklenmediyse fotoğrafı object storage'dan getirir.</summary>
    public async Task EnsureLoadedAsync()
    {
        if (Loaded)
        {
            return;
        }
        await ReloadAsync();
    }

    /// <summary>Fotoğrafı object storage'dan yeniden okur ve aboneleri bilgilendirir.</summary>
    public async Task ReloadAsync()
    {
        var photo = await _mediator.Send(new GetProfilePhotoQuery());
        PhotoDataUri = photo.IsSuccess && photo.Value is not null
            ? $"data:{photo.Value.ContentType};base64,{Convert.ToBase64String(photo.Value.Content)}"
            : null;
        Loaded = true;
        Changed?.Invoke();
    }

    /// <summary>
    /// Avatar'ı doğrudan verilen data URI ile günceller (foto yükleme sonrası,
    /// ek sorgu yapmadan). <c>null</c> → foto kaldırıldı.
    /// </summary>
    public void SetPhoto(string? dataUri)
    {
        PhotoDataUri = dataUri;
        Loaded = true;
        Changed?.Invoke();
    }
}
