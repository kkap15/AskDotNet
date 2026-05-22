import json
import os
from azure.ai.evaluation import evaluate
from azure.ai.evaluation import GroundednessEvaluator, RelevanceEvaluator, CoherenceEvaluator, FluencyEvaluator

model_config = {
    "azure_endpoint": os.environ["AZURE_OPENAI_ENDPOINT"],
    "api_key": os.environ["AZURE_OPENAI_API_KEY"],
    "azure_deployment": "gpt-4o-mini",
    "api_version": "2024-06-01"
}

with open("data/eval-report-raw.json") as f:
    results = json.load(f)
    
def build_dataset(results):
    dataset = [];
    for r in results:
        dataset.append({
            "query": r["Question"],
            "response": r["Answer"],
            "context": r["RetrievedContext"]
        })
    return dataset

dataset = build_dataset(results)

with open("data/foundry-dataset.jsonl", "w") as f:
    for item in dataset:
        f.write(json.dumps(item) + "\n")

result = evaluate(
    data = "data/foundry-dataset.jsonl",
    evaluators = {
        "groundedness": GroundednessEvaluator(model_config),
        "relevance": RelevanceEvaluator(model_config),
        "coherence": CoherenceEvaluator(model_config),
        "fluency": FluencyEvaluator(model_config)
    },
    evaluator_config = {
        "groundedness": {"column_mapping": {"query": "${data.query}", "response": "${data.response}", "context": "${data.context}"}},
        "relevance": {"column_mapping":  {"query": "${data.query}", "response": "${data.response}", "context": "${data.context}"}},
        "coherence": {"column_mapping": {"query": "${data.query}", "response": "${data.response}", "context": "${data.context}"}},
        "fluency": {"column_mapping": {"query": "${data.query}", "response": "${data.response}", "context": "${data.context}"}}
    }
)

print("\n====== Azure AI Foundry Evaluation Results ===\n")
metrics = result["metrics"]
for key, value in metrics.items():
    print(f"{key}: {value:.2f}")
    
with open("data/foundry-eval-results.json", "w") as f:
    json.dump(result, f, indent=2)
    
print ("\nFull results save to data/foundry-eval-results.json")
