
import pandas as pd
import matplotlib.pyplot as plt
import seaborn as sns
import numpy as np

MODE = "uncertainty"   # "clusters" oder "planck"

if MODE == "clusters":
    # ---------------------------------------------------------
    # 1. DATEN LADEN (Passe den Dateinamen an deine CSV an)
    # ---------------------------------------------------------
    # Angenommen, deine CSV heißt "results.csv" und hat die Spalten:
    # Cluster, z, MaxGradP, Improvement
    df = pd.read_csv('results.csv', sep=',') # Passe das Trennzeichen an, falls nötig (z.B. ',')

    # Unser gefundener magischer Schwellenwert
    THRESHOLD = 6.00E-034

    # ---------------------------------------------------------
    # 2. GRUPPEN DEFINIEREN (Für farbliche Unterscheidung)
    # ---------------------------------------------------------
    # Gruppe A: Newton (Rechts vom Threshold, Improvement sollte ~ 1.0 sein)
    # Gruppe B: TRM Support (Links vom Threshold, Improvement > 1.0)
    df['Theory'] = np.where(df['MaxGradP'] < THRESHOLD, 'TRM Support (Group B)', 'Newtonian Dynamics (Group A)')

    # ---------------------------------------------------------
    # 3. PLOT-DESIGN FÜR DAS PAPER
    # ---------------------------------------------------------
    plt.style.use('seaborn-v0_8-whitegrid')
    fig, ax = plt.subplots(figsize=(10, 7), dpi=300) # Hohe Auflösung für den Druck

    # Farben: Rot für TRM Support (Modifikation), Blau für Newton (Klassisch)
    colors = {'TRM Support (Group B)': '#e74c3c', 'Newtonian Dynamics (Group A)': '#3498db'}

    # Scatterplot zeichnen
    sns.scatterplot(
        data=df, 
        x='MaxGradP', 
        y='Improvement', 
        hue='Theory', 
        palette=colors,
        edgecolor='black',
        alpha=0.8,
        s=80, # Punktgröße
        ax=ax
    )

    # ---------------------------------------------------------
    # 4. DER PHYSIKALISCHE SCHWELLENWERT (Vertikale Linie)
    # ---------------------------------------------------------
    ax.axvline(x=THRESHOLD, color='black', linestyle='--', linewidth=2, label=f'Physical Threshold ($6.0 \\times 10^{{-34}}$)')

    # Achsen-Skalierung (Logarithmisch wegen der riesigen Spannen)
    ax.set_xscale('log')
    ax.set_yscale('log') # Wenn Improvement bis 250x geht, ist log-scale besser lesbar

    # Achsen-Beschriftungen (Wissenschaftlicher Standard)
    ax.set_xlabel('Maximum Core Pressure Gradient $\\nabla P$ (dyn/cm$^3$)', fontsize=14, fontweight='bold')
    ax.set_ylabel('Improvement Factor over Baseline (Error Reduction)', fontsize=14, fontweight='bold')

    # Beim Titel:
    ax.set_title('Bimodal Dynamics of Galaxy Clusters: Newton vs. Temporal Rate Matrix (TRM)', fontsize=16, pad=15)

    # Legende hübsch machen
    plt.legend(title='Dynamical Regime', title_fontsize='13', fontsize='11', loc='upper right', frameon=True, shadow=True)

    # Grid anpassen
    ax.grid(True, which="both", ls="--", alpha=0.5)

    # ---------------------------------------------------------
    # 5. SPEICHERN UND ANZEIGEN
    # ---------------------------------------------------------
    plt.tight_layout()
    plt.savefig('Clockwork_Threshold_Plot.png', bbox_inches='tight')
    plt.show()

    print("Plot erfolgreich erstellt und als 'Clockwork_Threshold_Plot.png' gespeichert.")

# =========================================================
# TQM PLANCK MULTI-TEST (NEU)
# =========================================================


elif MODE == "planck":

    df = pd.read_csv("planck_scan.csv")

    # Referenzwerte
    c0 = 299792458
    hbar0 = 1.054571817e-34
    G0 = 6.67430e-11

    # Normierung
    df["c_ratio"] = df["c"] / c0
    df["hbar_ratio"] = df["hbar"] / hbar0
    df["G_ratio"] = df["G"] / G0

    plt.style.use('seaborn-v0_8-whitegrid')
    fig, axs = plt.subplots(2, 2, figsize=(12, 10), dpi=300)

    axs[0,0].hist(df["c_ratio"], bins=50, color="#3498db")
    axs[0,0].set_title("Speed of Light Stability")
    axs[0,0].set_xlabel("c / c₀")
    axs[0,0].set_ylabel("Count")

    axs[0,1].scatter(df["epsL"], df["c_ratio"], s=5, alpha=0.5)
    axs[0,1].set_title("Influence of lₚ on c")
    axs[0,1].set_xlabel("εₗ")
    axs[0,1].set_ylabel("c / c₀")

    axs[1,0].scatter(df["epsL"], df["G_ratio"], s=5, alpha=0.5, color="#e74c3c")
    axs[1,0].set_title("Scaling of G")
    axs[1,0].set_xlabel("εₗ")
    axs[1,0].set_ylabel("G / G₀")

    axs[1,1].scatter(df["epsL"], df["hbar_ratio"], s=5, alpha=0.5, color="#2ecc71")
    axs[1,1].set_title("Scaling of ħ")
    axs[1,1].set_xlabel("εₗ")
    axs[1,1].set_ylabel("ħ / ħ₀")



    plt.tight_layout()
    plt.savefig("Planck_Sensitivity_Plot.png", bbox_inches='tight')
    plt.show()
    print(df.head())
    print("Planck Multi-Test Plot erstellt.")

elif MODE == "heatmap":

    import pandas as pd
    import numpy as np
    import matplotlib.pyplot as plt
    from scipy.ndimage import gaussian_filter

    df = pd.read_csv("planck_scan.csv")

    # -------------------------------
    # Referenzwerte
    # -------------------------------
    c0 = 299792458
    hbar0 = 1.054571817e-34
    G0 = 6.67430e-11

    # -------------------------------
    # Normierung
    # -------------------------------
    df["c_ratio"] = df["c"] / c0
    df["hbar_ratio"] = df["hbar"] / hbar0
    df["G_ratio"] = df["G"] / G0

    # -------------------------------
    # Stability Metric
    # -------------------------------
    df["stability"] = np.sqrt(
        (df["c_ratio"] - 1)**2 +
        (df["hbar_ratio"] - 1)**2 +
        (df["G_ratio"] - 1)**2
    )

    # -------------------------------
    # Minimum finden
    # -------------------------------
    min_row = df.loc[df["stability"].idxmin()]

    print("=== STABILITY MINIMUM ===")
    print(f"epsL = {min_row['epsL']}")
    print(f"epsT = {min_row['epsT']}")
    print(f"epsM = {min_row['epsM']}")
    print(f"stability = {min_row['stability']}")
    print(f"c_ratio = {min_row['c_ratio']}")
    print(f"hbar_ratio = {min_row['hbar_ratio']}")
    print(f"G_ratio = {min_row['G_ratio']}")

    # -------------------------------
    # Heatmap (Gitter erzeugen)
    # -------------------------------
    bins = 100

    heatmap_data, xedges, yedges = np.histogram2d(
        df["epsL"], df["epsT"],
        bins=bins,
        weights=df["stability"]
    )

    counts, _, _ = np.histogram2d(
        df["epsL"], df["epsT"],
        bins=bins
    )

    heatmap_avg = heatmap_data / np.maximum(counts, 1)

    # -------------------------------
    # GLÄTTUNG (WICHTIG!)
    # -------------------------------
    heatmap_avg = gaussian_filter(heatmap_avg, sigma=1.5)

    # -------------------------------
    # Gradient auf Gitter berechnen
    # -------------------------------
    grad_T, grad_L = np.gradient(heatmap_avg)

    # Grid erzeugen
    x = np.linspace(df["epsL"].min(), df["epsL"].max(), heatmap_avg.shape[1])
    y = np.linspace(df["epsT"].min(), df["epsT"].max(), heatmap_avg.shape[0])
    X, Y = np.meshgrid(x, y)

    # -------------------------------
    # Gradient normieren (nur Richtung!)
    # -------------------------------
    magnitude = np.sqrt(grad_L**2 + grad_T**2) + 1e-12

    step = 5  # weniger Pfeile = besser sichtbar

    # -------------------------------
    # KOMBINIERTER PLOT (BESTER!)
    # -------------------------------
    plt.figure(figsize=(8,6), dpi=300)

    # Heatmap
    plt.imshow(
        heatmap_avg.T,
        origin="lower",
        extent=[
            df["epsL"].min(), df["epsL"].max(),
            df["epsT"].min(), df["epsT"].max()
        ],
        cmap="inferno",
        alpha=0.85
    )

    # Flow-Feld (Richtung zur Stabilität)
    plt.quiver(
        X[::step, ::step], Y[::step, ::step],
        (-grad_L / magnitude)[::step, ::step],
        (-grad_T / magnitude)[::step, ::step],
        color="white",
        scale=20
    )

    # -------------------------------
    # Trajectory Simulation
    # -------------------------------

    def simulate_trajectory(start_x, start_y, steps=60, lr=0.005):
        x = start_x
        y = start_y

        traj_x = [x]
        traj_y = [y]

        for _ in range(steps):
            # nächster Grid-Punkt
            ix = np.argmin(np.abs(x_vals - x))
            iy = np.argmin(np.abs(y_vals - y))

            # Gradient an dieser Stelle
            gx = grad_L[iy, ix]
            gy = grad_T[iy, ix]

            # Bewegung Richtung Stabilität (negativer Gradient)
            x -= lr * gx
            y -= lr * gy

            traj_x.append(x)
            traj_y.append(y)

        return traj_x, traj_y


    # Grid separat speichern (für Simulation)
    x_vals = x
    y_vals = y


    # -------------------------------
    # Trajektorien Plotten
    # -------------------------------

    plt.figure(figsize=(8,6), dpi=300)

    # Hintergrund (Heatmap)
    plt.imshow(
        heatmap_avg.T,
        origin="lower",
        extent=[
            df["epsL"].min(), df["epsL"].max(),
            df["epsT"].min(), df["epsT"].max()
        ],
        cmap="inferno",
        alpha=0.85
    )

    # mehrere Startpunkte
    for _ in range(15):
        start_x = np.random.uniform(df["epsL"].min(), df["epsL"].max())
        start_y = np.random.uniform(df["epsT"].min(), df["epsT"].max())

        tx, ty = simulate_trajectory(start_x, start_y)

        plt.plot(tx, ty, color="white", alpha=0.8)

    # Minimum markieren
    plt.scatter(
        min_row["epsL"],
        min_row["epsT"],
        color="cyan",
        s=100,
        label="Minimum"
    )

    plt.legend()
    plt.colorbar(label="Stability Metric")

    plt.xlabel("εₗ (lP variation)")
    plt.ylabel("εₜ (tP variation)")
    plt.title("Stability Trajectories (Flow toward Minimum)")

    plt.tight_layout()
    plt.savefig("Trajectory_Flow.png", bbox_inches='tight')
    plt.show()

    # -------------------------------
    # TQF TRAJECTORY (mit Noise)
    # -------------------------------

    def simulate_tqf(start_x, start_y, steps=100, lr=0.003, noise_strength=0.001):
        x = start_x
        y = start_y

        traj_x = [x]
        traj_y = [y]

        for _ in range(steps):

            # Grid Index finden
            ix = np.argmin(np.abs(x_vals - x))
            iy = np.argmin(np.abs(y_vals - y))

            # Gradient holen
            gx = grad_L[iy, ix]
            gy = grad_T[iy, ix]

            # Zufällige Fluktuation (TQF)
            noise_x = np.random.normal(0, noise_strength)
            noise_y = np.random.normal(0, noise_strength)

            # Bewegung: Drift + Noise
            x -= lr * gx
            y -= lr * gy

            x += noise_x
            y += noise_y

            traj_x.append(x)
            traj_y.append(y)

        return traj_x, traj_y

    plt.figure(figsize=(8,6), dpi=300)

    # Heatmap Hintergrund
    plt.imshow(
        heatmap_avg.T,
        origin="lower",
        extent=[
            df["epsL"].min(), df["epsL"].max(),
            df["epsT"].min(), df["epsT"].max()
        ],
        cmap="inferno",
        alpha=0.85
    )

    # mehrere TQF-Pfade
    for _ in range(20):
        sx = np.random.uniform(df["epsL"].min(), df["epsL"].max())
        sy = np.random.uniform(df["epsT"].min(), df["epsT"].max())

        tx, ty = simulate_tqf(sx, sy)

        plt.plot(tx, ty, color="white", alpha=0.6)

    # Minimum markieren
    plt.scatter(
        min_row["epsL"],
        min_row["epsT"],
        color="cyan",
        s=100,
        label="Minimum"
    )

    plt.legend()
    plt.colorbar(label="Stability Metric")

    plt.xlabel("εₗ (lP variation)")
    plt.ylabel("εₜ (tP variation)")
    plt.title("TQF Trajectories (Fluctuations + Drift)")

    plt.tight_layout()
    plt.savefig("TQF_Trajectories.png", bbox_inches='tight')
    plt.show()

elif MODE == "uncertainty":

    import pandas as pd
    import matplotlib.pyplot as plt
    import numpy as np

    df = pd.read_csv("uncertainty_results.csv")

    plt.style.use("seaborn-v0_8-whitegrid")

    fig, axs = plt.subplots(2, 2, figsize=(12, 10), dpi=300)

    # ---------------------------------
    # Plot 1: DeltaE vs DeltaT
    # ---------------------------------
    axs[0, 0].plot(df["DeltaT"], df["DeltaE"], marker="o")
    axs[0, 0].set_xscale("log")
    axs[0, 0].set_yscale("log")
    axs[0, 0].set_title("Energy Uncertainty vs Observation Time")
    axs[0, 0].set_xlabel("Δt")
    axs[0, 0].set_ylabel("ΔE")

    # ---------------------------------
    # Plot 2: Product vs DeltaT
    # ---------------------------------
    axs[0, 1].plot(df["DeltaT"], df["Product"], marker="o", color="darkred")
    axs[0, 1].set_xscale("log")
    axs[0, 1].set_yscale("log")
    axs[0, 1].set_title("ΔE · Δt vs Observation Time")
    axs[0, 1].set_xlabel("Δt")
    axs[0, 1].set_ylabel("ΔE · Δt")

    # ---------------------------------
    # Plot 3: Temporal fluctuation std
    # ---------------------------------
    axs[1, 0].plot(df["DeltaT"], df["StdTemporalFluctuation"], marker="o", color="green")
    axs[1, 0].set_xscale("log")
    axs[1, 0].set_yscale("log")
    axs[1, 0].set_title("Std of Temporal Fluctuation vs Δt")
    axs[1, 0].set_xlabel("Δt")
    axs[1, 0].set_ylabel("σ(δT)")

    # ---------------------------------
    # Plot 4: Mean temporal fluctuation
    # ---------------------------------
    axs[1, 1].plot(df["DeltaT"], df["MeanTemporalFluctuation"], marker="o", color="purple")
    axs[1, 1].set_xscale("log")
    axs[1, 1].set_title("Mean Temporal Fluctuation vs Δt")
    axs[1, 1].set_xlabel("Δt")
    axs[1, 1].set_ylabel("mean(δT)")

    plt.tight_layout()
    plt.savefig("Uncertainty_Plots.png", bbox_inches="tight")
    plt.show()

    print(df.head())
    print("Uncertainty plots erstellt.")