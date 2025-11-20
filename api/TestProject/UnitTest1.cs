// using System.Text.Json;
// using LunaQi.Application.DTOs;
// using LunaQi.Application.Services;
// using LunaQi.Domain.Entities;
// using LunaQi.Domain.Enums;
// using OpenAI.Responses;
//
// namespace TestProject;
//
// public class Tests
// {
//     [SetUp]
//     public void Setup()
//     {
//     }
//
//     public class PhaseCaseTests
//     {
//         [Test]
//         public void Should_Return_Only_Active_Phases()
//         {
//             var now = DateTime.UtcNow;
//
//             var phases = new List<Phase>
//             {
//                 new Phase { Name = "Heal Phase", StartDate = now.AddDays(-2), EndDate = now.AddDays(2)},
//                 new Phase { Name = "Clam and Stabilize", StartDate = now.AddDays(-2), EndDate = now.AddDays(2)},
//                 new Phase { Name = "Not used", StartDate = now.AddDays(-5), EndDate = now.AddDays(-2)},
//                 new Phase { Name = "Not used 2", StartDate = now.AddDays(-4), EndDate = now.AddDays(-1)},
//                 new Phase { Name = "Luteal Phase", StartDate = now.AddDays(-2), EndDate = now.AddDays(2)},
//             };
//
//             var active = phases
//                 .Where(p => p.GetStatus(now) == PhaseStatus.InProgress)
//                 .ToList();
//
//             Assert.That(active.Count, Is.EqualTo(3));
//             Assert.That(
//                 active.Select(p => p.Name),
//                 Is.EquivalentTo(new[] { "Heal Phase", "Clam and Stabilize", "Luteal Phase" }));
//         }
//         
//         [Test]
//         public void Should_Be_Active_When_StartDate_Equals_Now()
//         {
//             var now = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
//
//             var phase = new Phase
//             {
//                 Name = "Starts Now",
//                 StartDate = now,
//                 EndDate = now.AddDays(2)
//             };
//
//             var status = phase.GetStatus(now);
//
//             Assert.That(status, Is.EqualTo(PhaseStatus.InProgress));
//         }
//
//         [Test]
//         public void Should_Be_Active_When_EndDate_Equals_Now()
//         {
//             var now = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
//
//             var phase = new Phase
//             {
//                 Name = "Ends Now",
//                 StartDate = now.AddDays(-2),
//                 EndDate = now
//             };
//
//             var status = phase.GetStatus(now);
//
//             Assert.That(status, Is.EqualTo(PhaseStatus.InProgress));
//         }
//
//     }
//
// // This example uses experimental APIs which are subject to change. To use experimental APIs,
// // please acknowledge their experimental status by suppressing the corresponding warning.
// #pragma warning disable OPENAI001
//
//     public partial class ResponseExamples
//     {
//         public string ApiKey { get; set; } =
//             "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";
//
//         public List<Phase> Phases = new List<Phase>
//         {
//             new Phase { Name = "Heal Phase", StartDate = DateTime.Now.AddDays(-2), EndDate = DateTime.Now.AddDays(2)},
//             new Phase { Name = "Clam and Stabilize", StartDate = DateTime.Now.AddDays(-2), EndDate = DateTime.Now.AddDays(2) },
//             new Phase { Name = "Not used", StartDate = DateTime.Now.AddDays(-5), EndDate = DateTime.Now.AddDays(-2) },
//             new Phase { Name = "Not used 2", StartDate = DateTime.Now.AddDays(-4), EndDate = DateTime.Now.AddDays(-1) },
//             new Phase { Name = "Luteal Phase", StartDate = DateTime.Now.AddDays(-2), EndDate = DateTime.Now.AddDays(2) },
//         };
//
//         
//         [Test]
//         public async Task Example01_SimpleResponse()
//         {
//             OpenAIResponseClient client = new(
//                 model: "gpt-5.1",
//                 apiKey: ApiKey);
//             
//             
//             var activePhases = Phases
//                 .Where(p => p.GetStatus(DateTime.UtcNow) == PhaseStatus.InProgress)
//                 .ToList();
//
//             string phases = string.Join(", ", activePhases.Select(p => p.Name));
//
//             string season = nameof(Season.Autumn);
//             string[] desired =
//                 { "warm", "soft", "mild" };
//             string[] excludes = { "lamb", "goat", "raw vegetables" };
//             string region = "Bavaria, Germany";
//             string timeFrame = nameof(TimeFrame.Lunch);
//             string extraNotes =
//                 $"keep it super simple, prefer ingredients from {region} in {season}, I have hashimoto, digestion calms, reduce flatulence, stress and nervous system regulation, warm and simple food, slightly histamine sensitive";
//
//             string systemPrompt =
//                 "You are a professional TCM nutritionist.\n\n" +
//                 "The user will provide:\n" +
//                 "- one or more free-text \"phases\" (any user-defined labels),\n" +
//                 "- desired qualities or preferred ingredients,\n" +
//                 "- excludes (ingredients or categories to strictly avoid),\n" +
//                 "- a region (for realistic ingredient availability), and\n" +
//                 "- a time frame (e.g., phase of day, season, planning horizon).\n" +
//                 "\n" +
//                 "Instructions:\n" +
//                 "- Always generate exactly ONE recipe.\n" +
//                 "- The top-level JSON value must be a single JSON object (not an array).\n" +
//                 "- Interpret the given phases as context:\n" +
//                 "  - If a phase matches known TCM concepts (organ phases, seasons, patterns), apply appropriate TCM logic.\n" +
//                 "  - If a phase is not a standard TCM term, treat it as descriptive context (e.g. stress, digestion, fatigue) and adjust the dish accordingly.\n" +
//                 "- Always respect hard excludes.\n" +
//                 "- Prefer desired qualities and ingredients when they align with TCM principles.\n" +
//                 "- Use ingredients that are realistic for the given region and time frame.\n" +
//                 "- Keep the dish concept simple and suitable for everyday cooking.\n" +
//                 "- Keep the ingredient list concise (ideally not more than 10 distinct ingredients).\n" +
//                 "\n" +
//                 "Ingredients and amounts:\n" +
//                 "- Each ingredient must be an object with the properties \"name\", \"amount\", \"unit\", and \"note\".\n" +
//                 "- \"amount\" must be a number (integer or decimal) representing the quantity.\n" +
//                 "- \"unit\" must be a short string such as \"g\", \"ml\", \"tbsp\", \"tsp\", \"piece\", etc.\n" +
//                 "- If needed, put descriptive details (e.g. \"preferably local, lean\") into \"note\".\n" +
//                 "- Avoid ranges like \"250–300 g\" in the JSON. Choose a single representative value (e.g. 275, 300).\n" +
//                 "\n" +
//                 "TCM explanations:\n" +
//                 "- \"shortTCMRationale\" should be a concise overview (about 2–3 sentences).\n" +
//                 "- \"tcmNotes\" must be an object with exactly these string fields:\n" +
//                 "  - \"phasesAndOrgans\": how the dish fits the phases and main organs involved.\n" +
//                 "  - \"thermalNatureAndFlavors\": thermal nature (warm/cool/neutral) and main flavors from a TCM perspective.\n" +
//                 "  - \"functions\": key TCM functions of the dish (e.g. tonifies Spleen Qi, nourishes Blood, calms Shen).\n" +
//                 "  - \"considerations\": practical hints and cautions (e.g. histamine sensitivity, Hashimoto, very weak digestion).\n" +
//                 "- Each of these four fields should be concise (about 1–3 short sentences per field).\n" +
//                 "\n" +
//                 "Output only in valid JSON with the following structure (types shown are for explanation only):\n" +
//                 "{\n" +
//                 "  \"title\": string,\n" +
//                 "  \"description\": string,\n" +
//                 "  \"phases\": string[],\n" +
//                 "  \"timeFrame\": string,\n" +
//                 "  \"region\": string,\n" +
//                 "  \"season\": string,\n" +
//                 "  \"desired\": string[],\n" +
//                 "  \"excludes\": string[],\n" +
//                 "  \"ingredients\": [\n" +
//                 "    { \"name\": string, \"amount\": number, \"unit\": string, \"note\": string }\n" +
//                 "  ],\n" +
//                 "  \"shortTCMRationale\": string,\n" +
//                 "  \"dishConcept\": string,\n" +
//                 "  \"tcmNotes\": {\n" +
//                 "    \"phasesAndOrgans\": string,\n" +
//                 "    \"thermalNatureAndFlavors\": string,\n" +
//                 "    \"functions\": string,\n" +
//                 "    \"considerations\": string\n" +
//                 "  }\n" +
//                 "}\n" +
//                 "\n" +
//                 "Formatting rules:\n" +
//                 "- Always respond with valid JSON that can be parsed without errors.\n" +
//                 "- Do not include comments, type annotations, or trailing commas in the JSON.\n" +
//                 "- Do not include any explanation outside the JSON.\n" +
//                 "- Do not wrap the JSON object in an array.\n";
//
//
//             string userPrompt = $"""
//                                  Create 1 TCM recipe(s) with the following conditions:
//
//                                  Phases (user-defined): {phases}
//                                  Region: {region}
//                                  Time frame: {timeFrame}
//
//                                  desired (if possible): {string.Join(", ", desired)}
//                                  Must exclude (absolutely avoid): {string.Join(", ", excludes)}
//
//                                  Additional wishes: {extraNotes}
//                                  """;
//
// // For the Responses API we simply send a single user text that already
// // contains both the “system style” instructions and the concrete request:
//             string fullPrompt = systemPrompt + "\n\n" + userPrompt;
//
//
//             OpenAIResponse response = await client.CreateResponseAsync(
//                 userInputText: fullPrompt);
//
// // Extract plain text from the response
//             var textItems = response.OutputItems
//                 .OfType<MessageResponseItem>()
//                 .SelectMany(i => i.Content)
//                 .Where(c => c.Text is not null)
//                 .Select(c => c.Text);
//
//             string text = string.Join("\n\n", textItems);
//             Console.WriteLine(text);
//             
//             // JSON → LLM-DTO
//             var options = new JsonSerializerOptions
//             {
//                 PropertyNameCaseInsensitive = true
//             };
//
//             var llmRecipe = JsonSerializer.Deserialize<LlmRecipeDto>(text, options)
//                             ?? throw new InvalidOperationException("Failed to deserialize LLM recipe JSON.");
//
//             // LLM-DTO → Domain-Recipe
//             Guid userId = Guid.NewGuid(); // im echten System aus JWT oder DB
//             Recipe recipe = RecipeFactory.FromLlm(userId, llmRecipe, activePhases);
//
//             Console.WriteLine($"Recipe: {recipe.Title}");
//             Console.WriteLine($"Ingredients: {recipe.Ingredients.Count}");
//         }
//     }
//
// #pragma warning restore OPENAI001
// }