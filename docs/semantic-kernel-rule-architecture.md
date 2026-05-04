# Semantic Kernel Rule Architecture

## Overview

The Rules service now uses Microsoft Semantic Kernel as the primary rule-execution layer. RabbitMQ subscriptions still receive all smart-home events, but legacy hard-coded and monitor rules are no longer executed directly. Instead, their behavior is represented as prompt rules inside a single smart-home system prompt.

## Main Components

- `SmartHomeKernelService`
  - receives every `NetworkEvent` from `RuleService`
  - builds a Semantic Kernel instance with Azure OpenAI chat completion
  - loads the last 48 hours of conversation history
  - appends the current event as a user turn
  - invokes the kernel with auto function calling enabled

- `SmartHomePromptProvider`
  - contains the smart-home system prompt
  - contains the migrated legacy rules from `HardCodedRuleStore` and `MonitoringRuleStore` as prompt rules

- `SmartHomePromptRuleStore`
  - keeps prompt rules visible through the existing `IRuleStore` / `IRuleService` API
  - exposes timer rules so the existing timer engine still emits timer events

- Kernel plugins
  - `SpeechKernelPlugin`: publishes `SayMessage`
  - `AudioKernelPlugin`: publishes `StartAudioMessage` and `StopAudioMessage`
  - `DeviceControlKernelPlugin`: controls scenes, lamps, wall plugs and thermostats via `DeviceServiceClient`

## Event Flow

1. `RuleService` receives an event from RabbitMQ.
2. `RuleService` maps the message to a typed `NetworkEvent`.
3. `RuleService` forwards the event to `SmartHomeKernelService`.
4. `SmartHomeKernelService` loads recent history, builds the event prompt and invokes the kernel.
5. The kernel decides whether to call plugins or to do nothing.
6. Plugin calls perform speech or device control actions.
7. User and assistant messages are persisted for future context.

## Conversation History

- A single conversation ID is used for the whole smart home.
- History is stored in `KernelConversationMessages`.
- Messages older than 48 hours are deleted before each invocation.
- The retained history gives the model short-term memory for presence, weather and earlier state changes.

## Safety Model

- The system prompt instructs the model to act only through plugins.
- Speech output must be produced through the speech plugin, not only as plain text.
- Device actions are restricted to typed plugin functions that map to existing service-client operations.
- Legacy rule intent remains encoded in prompt rules to preserve expected home behavior.