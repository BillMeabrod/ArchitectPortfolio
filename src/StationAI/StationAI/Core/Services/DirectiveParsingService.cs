using Station.Logging;
using StationAI.Core.Interfaces;
using StationAI.Core.Models;
using StationAI.Core.Models.Constants;
using System.Text.Json;

namespace StationAI.Core.Services;

public class DirectiveParsingService : IDirectiveParsingService
{
    private readonly ILargeLanguageModelService _llmService;
    private readonly IStationLogger<DirectiveParsingService> _log;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public DirectiveParsingService(
        ILargeLanguageModelService llmService,
        IStationLogger<DirectiveParsingService> log)
    {
        _llmService = llmService;
        _log = log;
    }

    public async Task<IReadOnlyList<DirectiveTarget>> Parse(string directive)
    {
        if (string.IsNullOrWhiteSpace(directive))
            return [];

        string prompt = BuildPrompt(directive);
        string response;

        _log.Info("Parsing directive targets — directive length: {Length} chars", directive.Length);
        _log.Info("Directive parsing prompt:\n{Prompt}", prompt);

        try
        {
            response = await _llmService.SendPrompt(prompt, typeof(List<DirectiveTarget>));
        }
        catch (Exception ex)
        {
            _log.Warn("Directive parse failed: LLM call threw unexpectedly. Directive length: {Length}. No targets stored.", directive.Length);
            _log.Error(ex, "Directive parse LLM exception");
            return [];
        }

        List<DirectiveTarget>? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<List<DirectiveTarget>>(response, SerializerOptions);
        }
        catch (JsonException ex)
        {
            _log.Warn("Directive parse failed: LLM returned unparseable JSON. No targets stored.");
            _log.Error(ex, "Directive parse JSON exception. Raw response: {Response}", response);
            return [];
        }

        if (parsed is null)
        {
            _log.Warn("Directive parse failed: LLM returned null. No targets stored.");
            return [];
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
                _log.Warn("Directive parse produced a target with an empty Target value; discarding. Type: {Type}, Concern: {Concern}",
                    candidate.Type, candidate.Concern);
                continue;
            }

            if (!LoreCategories.IsValid(candidate.Type))
            {
                _log.Warn("Directive parse produced target '{Target}' with unrecognized category '{Type}'; discarding.",
                    candidate.Target, candidate.Type);
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
            {{directive}}
            """;
    }
}