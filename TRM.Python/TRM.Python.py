
import pandas as pd
import matplotlib.pyplot as plt
import seaborn as sns
import numpy as np

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