using Microsoft.Extensions.Logging;
using StationAI.Core.Interfaces;
using StationAI.Core.Models;
using StationAI.Core.Models.Constants;
using System.Text.Json;

namespace StationAI.Core.Services;

public class DirectiveParsingService : IDirectiveParsingService
{
    private readonly ILargeLanguageModelService _llmService;
    private readonly ILogger<DirectiveParsingService> _logger;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public DirectiveParsingService(
        ILargeLanguageModelService llmService,
        ILogger<DirectiveParsingService> logger)
    {
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DirectiveTarget>> Parse(string directive)
    {
        if (string.IsNullOrWhiteSpace(directive))
            return [];

        string prompt = BuildPrompt(directive);

        string response = await _llmService.SendPrompt(prompt, typeof(List<DirectiveTarget>));

        List<DirectiveTarget>? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<List<DirectiveTarget>>(response, SerializerOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Directive parse failed: language model returned unparseable JSON. Raw response: {Response}",
                response);
            throw new InvalidOperationException(
                "Failed to parse station directive: language model returned unparseable output.", ex);
        }

        if (parsed is null)
        {
            _logger.LogError(
                "Directive parse failed: language model returned null. Raw response: {Response}",
                response);
            throw new InvalidOperationException(
                "Failed to parse station directive: language model returned null.");
        }

        return Validate(parsed);
    }

    private IReadOnlyList<DirectiveTarget> Validate(List<DirectiveTarget> candidates)
    {
        var valid = new List<DirectiveTarget>(candidates.Count);

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate.Target))
            {
                _logger.LogWarning(
                    "Directive parse produced a target with an empty Target value; discarding. Type: {Type}, Concern: {Concern}",
                    candidate.Type, candidate.Concern);
                continue;
            }

            if (!LoreCategories.IsValid(candidate.Type))
            {
                _logger.LogWarning(
                    "Directive parse produced target '{Target}' with unrecognized category '{Type}'; discarding. Valid categories: {Categories}",
                    candidate.Target, candidate.Type, string.Join(", ", LoreCategories.All));
                continue;
            }

            candidate.Type = candidate.Type.ToLowerInvariant();
            valid.Add(candidate);
        }

        return valid;
    }

    private static string BuildPrompt(string directive)
    {
        string categoryList = string.Join(", ", LoreCategories.All);

        return $$"""
            You convert a space station's intelligence directive, written in natural language,
            into a structured list of retrieval targets. Each target names one specific entity
            the directive wants watched, so it can be located in a lore database.

            For each distinct target, output an object with:
            - "target": a short, plain noun phrase naming the entity itself, with NO instruction
              words. Good: "Crimson Syndicate". Bad: "flag the Crimson Syndicate". This text is
              used to search a database, so it must be pure content describing the entity.
            - "type": exactly one of these categories: {{categoryList}}. Choose the best fit.
            - "concern": a brief phrase capturing why the directive references this target
              (e.g. "hostile faction", "contraband", "person of interest"). Context only.

            Rules:
            - Extract every distinct target the directive names.
            - Do NOT include dispositions, exceptions, amnesties, or instructions in any field.
              Only identify the targets and why they are referenced.
            - If a target does not clearly fit one of the listed categories, omit it.
            - Output ONLY a JSON array of these objects. No prose, no markdown, no code fences.

            Directive:            
            {{ directive}}
            """;
    }
}