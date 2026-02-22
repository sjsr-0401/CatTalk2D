# 🐱 CatTalk2D — AI-Powered Virtual Cat Companion

A Unity-based desktop pet game where a virtual cat powered by **local LLMs** (via Ollama) develops its own personality, remembers conversations, and reacts with contextual emotions and behaviors.

![Unity](https://img.shields.io/badge/Unity-2D-black?logo=unity)
![C#](https://img.shields.io/badge/C%23-Scripts-239120?logo=csharp)
![Ollama](https://img.shields.io/badge/Ollama-Local_LLM-white)
![License](https://img.shields.io/badge/license-MIT-green)

---

## 📸 Screenshots

<!-- TODO: Add screenshots / GIFs -->
> _Screenshots coming soon_

---

## 💡 What Makes This Different

This isn't a chatbot with a cat skin. The cat has:

- **Memory** — Remembers past conversations and builds context over time
- **Mood & State** — Hunger, trust level, and emotional state affect responses
- **Trust Tiers** — The cat warms up to you gradually (stranger → acquaintance → friend)
- **Autonomous Behavior** — Monologues, idle actions, and reactions without user input
- **100% Local AI** — No cloud APIs, no data leaves your machine

---

## ✨ Features

### AI & Personality
- Local LLM integration via **Ollama** (supports Gemma, Qwen, Aya, and custom LoRA models)
- **Structured Control System** — AI outputs JSON control signals that drive behavior, not just text
- **Sentiment Analysis** — Parses emotional tone to adjust cat state
- **Memory Manager** — Summarizes and retains conversation history across sessions
- **Prompt Engineering** — Dynamic prompt builder that injects state, memory, and personality

### Cat Simulation
- Hunger system with food bowl interaction
- Trust tier progression (affects response style and available interactions)
- Day/night cycle awareness
- Idle monologues and autonomous behavior planning

### Development Tools (Built-in)
- **Benchmark Runner** — Measures response quality with cat-likeness scoring
- **Training Data Generator** — Auto-generates fine-tuning datasets from interactions
- **LoRA Training Pipeline** — Full workflow from data → Unsloth training → GGUF conversion → deployment

---

## 🏗 Architecture

```
┌─────────────────────────────────────────────┐
│                 Unity (Game)                 │
│                                             │
│  ChatUI ──→ InputHandler ──→ PromptBuilder  │
│                                      │      │
│  CatStateManager ◄── ControlBuilder  │      │
│       │                     ▲        │      │
│  CatMemoryManager          │        ▼      │
│       │              ResponseProcessor      │
│       ▼                     ▲               │
│  InteractionLogger    OllamaAPIManager      │
│                             │               │
└─────────────────────────────┼───────────────┘
                              │
                    ┌─────────▼─────────┐
                    │   Ollama Server    │
                    │  (Local LLM)      │
                    └───────────────────┘
```

### Key Scripts

| Script | Role |
|--------|------|
| `PromptBuilder.cs` | Assembles system prompt with state, memory, personality |
| `OllamaAPIManager.cs` | HTTP client for Ollama API |
| `ResponseProcessor.cs` | Parses JSON control signals from LLM output |
| `CatStateManager.cs` | Manages hunger, trust, mood |
| `CatMemoryManager.cs` | Conversation memory with summarization |
| `BehaviorPlan.cs` | Autonomous behavior planning |
| `SentimentAnalyzer.cs` | Emotional tone detection |
| `MonologueManager.cs` | Idle speech / thought bubbles |

---

## 🚀 Getting Started

### Prerequisites
- Unity 2022.3+ (LTS)
- [Ollama](https://ollama.ai) installed and running
- A compatible model pulled (e.g., `ollama pull gemma2:2b`)

### Setup
```bash
git clone https://github.com/sjsr-0401/CatTalk2D.git
```
1. Open project in Unity
2. Start Ollama: `ollama serve`
3. Pull a model: `ollama pull gemma2:2b`
4. Press Play in Unity Editor

### Custom LoRA Model (Optional)
See [docs/TECHNICAL_JOURNEY.md](docs/TECHNICAL_JOURNEY.md) for the full fine-tuning pipeline:
1. Generate training data from interactions
2. Fine-tune with Unsloth
3. Convert to GGUF
4. Deploy via Ollama

---

## 📁 Project Structure

```
CatTalk2D/
├── Assets/_Project/
│   └── Scripts/
│       ├── AI/           # LLM integration, prompts, response parsing
│       ├── Cat/          # State, behavior, movement, interaction
│       ├── Memory/       # Conversation history & summarization
│       ├── UI/           # Chat interface, bubbles, food bowl
│       └── Dev/          # Benchmark, training data tools
├── docs/
│   ├── ARCHITECTURE.md   # Full system architecture
│   ├── TECHNICAL_JOURNEY.md  # Development log & decisions
│   └── GGUF_QUANTIZATION.md  # Model conversion guide
├── LoraData/             # Fine-tuning datasets
└── training_data.jsonl   # Generated training samples
```

---

## 🧠 Technical Highlights

- **No cloud dependency** — Privacy-first design, all AI runs locally
- **Structured output** — LLM generates JSON control signals, not free-form text, ensuring deterministic game behavior
- **Memory architecture** — Rolling summary + recent context window for coherent long conversations
- **Modular AI backend** — Swap models without changing game code (Gemma → Qwen → custom LoRA)
- **Built-in evaluation** — Cat-likeness scoring benchmarks for comparing model performance

---

## 📄 License

MIT License

---

Built by [Kim Seongjine](https://github.com/sjsr-0401) — Exploring the intersection of game development and local AI.
