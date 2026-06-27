# TRM Finsler-Like and Higher-Order Optical Action

## Scope

This document formalizes the mathematical status of the TRM photon transport action in a reviewer-safe way.
It does **not** claim that TRM is already a closed Finsler theory.

---

## 1. Optical Action Used in TRM

We use an optical action of the form:

\[
S = \int L(x,\dot x,\ddot x)\,ds
\]

with:

\[
L = n_{\mathrm{eff}}(x,\dot x,\ddot x)
\]

and the implemented transport index:

\[
n_{\mathrm{eff}}
=
2
+
\lambda_t\phi
+
\lambda_s\phi^2 |\dot\mu|
\]

Here:

\[
\phi(r)=\frac{GM}{c^2 r},
\qquad
\mu = \hat v\cdot\hat r
\]

Because \(\dot\mu\) depends on directional change, the optical action is naturally higher-order in path kinematics.

---

## 2. Standard Finsler vs. Current TRM Form

### Standard Finsler-Type Structure

A standard Finsler-type action has the form:

\[
F = F(x,\dot x)
\]

with action:

\[
S_F = \int F(x,\dot x)\,ds
\]

### Current TRM Structure

The current TRM transport form is more naturally written as:

\[
F = F(x,\dot x,\ddot x)
\]

through the \(\phi^2|\dot\mu|\) channel in \(n_{\mathrm{eff}}\).

Therefore the current transport action is more conservative to describe as a **Finsler-like or higher-order optical action** on a fixed Euclidean base space.

---

## 3. Resulting Euler-Lagrange Level

For a Lagrangian of the form \(L(x,\dot x,\ddot x)\), the stationary-path condition leads to a higher-order Euler-Lagrange equation:

\[
\frac{\partial L}{\partial x_i}
-
\frac{d}{ds}\frac{\partial L}{\partial \dot x_i}
+
\frac{d^2}{ds^2}\frac{\partial L}{\partial \ddot x_i}
=0
\]

This is the formal reason why TRM transport dynamics cannot be represented by a purely position-only optical medium.

---

## 4. Connection to Executable Bridge Tests

- EL01–EL17: executable EL/Fermat bridge track with weak-field validation windows.
- CML01–CML08: isolated collective mode-locking track and bridge-scale band mapping.
- MEM01: memory-channel ablation test showing that the memory channel is not a Shapiro-time contribution and has its strongest current role in the EL/Fermat bridge behavior.

These tests validate bounded behavior and bridge consistency, but they do not yet provide a full first-principles closure of the higher-order action.

---

## 5. Claim Boundary (Review-Safe)

Use this wording in review-facing documents:

> TRM photon transport admits a Finsler-like or higher-order optical-action formulation on a fixed Euclidean base space.

Avoid stronger wording such as:

> TRM is Finsler geometry.

until a full geometric closure and uniqueness conditions are derived.
