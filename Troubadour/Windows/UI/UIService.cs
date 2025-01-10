using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;
using Troubadour.BGM;

namespace Troubadour.Services;

public class UIService
{
    private readonly ITextureProvider textureProvider;
    private readonly List<BgmData> bgmList;
    private readonly Dictionary<ushort, bool> bgmStates;

    public UIService(ITextureProvider textureProvider, List<BgmData> bgmList, Dictionary<ushort, bool> bgmStates)
    {
        this.textureProvider = textureProvider;
        this.bgmList = bgmList;
        this.bgmStates = bgmStates;
    }

    /// <summary>
    /// Retrieves an icon for the specified ID.
    /// </summary>
    /// <param name="iconID">The ID of the icon to retrieve.</param>
    public ISharedImmediateTexture? GetIcon(uint iconID)
    {
        try
        {
            return textureProvider.GetFromGameIcon(iconID);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Filters the list of BGM entries based on the specified search term, category, and state check.
    /// </summary>
    /// <param name="search">The search term to filter BGM names.</param>
    /// <param name="category">The category to filter BGM extensions.</param>
    /// <param name="checkStates">Indicates whether to filter by the state of the BGM entries.</param>
    public List<BgmData> GetFilteredBgm(string search, string category, bool checkStates)
    {
        return bgmList
            .Where(bgm =>
                HasValidDuration(bgm) &&
                MatchesSearch(bgm, search) &&
                MatchesCategory(bgm, category) &&
                MatchesState(bgm, checkStates))
            .ToList();

        // checks if the duration is valid
        bool HasValidDuration(BgmData bgm) => bgm.Duration > TimeSpan.Zero;

        // checks if the BGM matches the search criteria
        bool MatchesSearch(BgmData bgm, string search) =>
            string.IsNullOrWhiteSpace(search) ||
            bgm.English.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            bgm.Id.ToString().Contains(search, StringComparison.OrdinalIgnoreCase);

        // checks if the BGM matches the specified category
        bool MatchesCategory(BgmData bgm, string category) =>
            string.IsNullOrWhiteSpace(category) ||
            string.IsNullOrWhiteSpace(bgm.Extension) ||
            string.Equals(bgm.Extension, category, StringComparison.OrdinalIgnoreCase);

        // checks if the BGM is checked
        bool MatchesState(BgmData bgm, bool checkStates) =>
            !checkStates ||
            (bgmStates.ContainsKey(bgm.Id) && bgmStates[bgm.Id]);
    }

    /// <summary>
    /// Gets the state of the specified BGM entry.
    /// </summary>
    /// <param name="bgmId">The ID of the BGM entry.</param>
    /// <returns>True if the BGM entry is marked as active in the states dictionary; otherwise, false.</returns>
    public bool GetBgmState(ushort bgmId)
    {
        return bgmStates.ContainsKey(bgmId) && bgmStates[bgmId];
    }

    /// <summary>
    /// Sets the state of the specified BGM entry.
    /// </summary>
    /// <param name="bgmId">The ID of the BGM entry.</param>
    /// <param name="state">The new state to assign to the BGM entry.</param>
    public void SetBgmState(ushort bgmId, bool state)
    {
        bgmStates[bgmId] = state;
    }
}
