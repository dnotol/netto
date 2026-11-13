---
type: glossary
title: French words of Netto's interface
description: The French term Netto's interface uses for each English one, the official Belgian term where the administration has one, with its source; Netto's own names fixed once; how French text is written.
tags: [site, language]
sources:
  - id: research
    resource: ../research/2026-10-30-french-tax-terms.md
generated: {by: claude-opus-5-5/knowledge-base, at: 2026-10-30T18:00:00+01:00}
---

# How French is written

- « vous » throughout, as SPF Finances and INASTI write. Sentence case: only a heading's first word takes a capital.
- Amounts read like the administration's: a dot for thousands, a comma for decimals, the unit after the number with a no-break space: « 1.234,56 € », « 51.000 € ». Percentages: « 20,5 % ». A figure written inside a sentence follows the same form.
- No-break space (U+00A0, `&#160;` in a resx) before `: ; ! ?` and `%`, and inside « ». Apostrophes are straight (`'`), as in the English files.
- Dates: « 1er janvier 2027 », « 10 avril 2026 », months in lower case; short months « janv., févr., mars, avr., mai, juin, juil., août, sept., oct., nov., déc. ».
- Income year and tax year: « revenus 2026 », « exercice d'imposition 2027 (revenus 2026) ». Never « année d'imposition ».
- A month name or a municipality name filled in by code never follows « de » (« d'avril », « de Ath » cannot be built): write « entre {0} et {1} », « en {0} », « Commune : {0} ».
- An amount filled in by code is never the subject of a participle that must agree: write a noun (« Versements anticipés : {0} »), not « {0} payés ».
- « majoration » is only the advance-payment surcharge. The additions to the tax-free amount are « suppléments »; the municipal surcharge is the « taxe communale additionnelle ».
- English « withholding » is two French terms: on pay « précompte professionnel », on a dividend or a royalty « précompte mobilier ».

# Netto's own names

Fixed once; every file that names one uses it word for word (the card, the map, the Optimise view, the menu, the tab bar).

| English | French |
|---|---|
| Company year | Exercice de la société |
| Vouchers | Titres-repas et chèques |
| Expense allowances | Indemnités de frais |
| Home office rent | Loyer du bureau |
| Company car | Voiture de société |
| The split (the pay-versus-dividend concept) | La répartition |
| To share (the node of the map: what the levers leave to split) | À partager |
| Copyright royalty | Droits d'auteur |
| Director pay | Rémunération de dirigeant |
| Household / Your household | Ménage / Votre ménage |
| Corporate tax | Impôt des sociétés |
| Dividend | Dividende |
| Income year (the whole river) | Revenus {year}, or « L'année » where no year is shown |
| Your pocket | Votre poche |
| Contributions and income tax (one name whatever the plan: the pay, a rent and a royalty) | Cotisations et IPP (IPP is l'impôt des personnes physiques, spelled out in the bucket's bubble; short, to fit the bar on one line) |
| Taxes on profit | Impôts sur le bénéfice |
| Kept in company | Gardé dans la société |
| Expenses | Frais |
| My plan · Optimise · Ideas (the three views of the plan page) | Mon plan · Optimiser · Pistes |
| When to pay (the section of Optimise that holds the payment calendar, and the spoken name of its link in the band) | Quand payer |
| Compare plans (the Plans panel's button and its comparison) · Back to plans | Comparer les plans · Retour aux plans |
| next advance payment by {date} (the link to When to pay beside Optimise's tab, spoken after « When to pay: ») | Quand payer : prochain versement anticipé au plus tard le {date} (non-breaking space before the colon) |
| what the fund bills is refused (the same link, spoken) | Quand payer : le montant facturé par votre caisse est refusé |
| Optimise can add {amount} without changing anything else (My plan's nudge) | Optimiser peut ajouter {amount} sans rien changer d'autre |
| Ideas: {n} priced, {m} questions (My plan's nudge; the badge shows « {n} · {m}? » and says the counts alone; a count of zero is left out) | Pistes : {n} chiffrée(s), {m} question(s) (agreement by key, one or many; a count of zero is left out) |
| page titles: My plan · Optimise · Ideas, each followed by « : Netto » | Mon plan · Optimiser · Pistes, each followed by « : Netto » (non-breaking space before the colon) |
| Your plans · Save this plan · Add to plan | Vos plans · Enregistrer ce plan · Ajouter au plan |
| Today (the starred plan) | Aujourd'hui (a name, capitalised: « de plus qu'Aujourd'hui ») |
| the river (the plan on screen) | not translated as a metaphor: « le plan », « le plan affiché » |
| a move (Optimise) | un ajustement |
| a lever | un levier |
| an idea (Ideas: a lever not in the plan) | une piste |
| Try · Keep · Drop (an idea) | Essayer · Garder · Abandonner |
| a trial (of an idea, inside the Ideas view) | un essai |
| if yes (an idea's gain before the viewer's yes) | si oui |
| measuring ideas (the Ideas badge, spoken, while one is measured) | chiffrage des pistes |
| not in Netto yet (what Netto does not model, no promise) | pas encore dans Netto |
| Car's benefit in kind before / after your contribution | Avantage de toute nature de la voiture avant / après votre intervention |
| tested pay (art. 215) | rémunération prise en compte |
| reserve (cash the company keeps) | montant gardé dans la société (never « réserve », which is a legal term) |
| a bucket of the bar | un compartiment |
| the map · a card | la carte · la carte named by its module (« la carte Rémunération de dirigeant ») |
| a rate's condition met / not met (on the map) | oui / non |
| the rent's floor | le plancher |
| the fund's management fee | les frais de gestion de la caisse |
| a bill (in the Chamber) | un projet de loi |
| the picture of the plan (the map or the Sankey) · Map · Sankey | l'image du plan · Carte · Sankey |
| a band (of the Sankey) | une bande |
| profits kept from earlier years, after tax (the Sankey's node and the Dividend card's field) | réserves taxées des années précédentes |

# Official terms

The French is the administration's wording; the source is the filed document in `official-docs/` the research quotes (see [the research note](../research/2026-10-30-french-tax-terms.md) for each quote).

## Company and corporate tax

| English | French | Source |
|---|---|---|
| corporate tax | l'impôt des sociétés (ISoc) | CIR 92 art. 215 |
| small company | la petite société | art. 215, 218 |
| reduced rate · standard rate | le taux réduit · le taux de base | commentaire art. 215 (« taux de base réduit ») |
| minimum director pay | la rémunération minimale | art. 215 al. 3, 4° |
| tax period | la période imposable | art. 215 |
| financial year | l'exercice comptable | art. 218, 269 |
| tax year · income year | l'exercice d'imposition · les revenus (2026) | SPF versements anticipés sociétés |
| taxable income | le revenu imposable | art. 215, 130 |
| disallowed expenses · added back | les dépenses non admises · repris en dépenses non admises | cours ISoc 5.3; art. 198 |
| balance-sheet total | le total du bilan | INASTI FAQ cotisation des sociétés |
| INASTI's company contribution | la cotisation à charge des sociétés | INASTI FAQ |
| benefits in kind valued at a flat rate | les avantages de toute nature évalués de manière forfaitaire | art. 2 |
| liquidation reserve | la réserve de liquidation | art. 21 11° |
| profits kept from earlier years | les réserves (taxées) | art. 215, 269 |
| 281.45 fiche | la fiche 281.45 | art. 57 |

## Advance payments and the assessment

| English | French | Source |
|---|---|---|
| advance payment (1st…4th) | le versement anticipé (VA1…VA4) | SPF versements anticipés indépendants |
| advance-payment surcharge | la majoration (d'impôt) pour absence ou insuffisance de versements anticipés | art. 218; SPF |
| bonus | la bonification | art. 176 |
| reference rate | le taux de référence | art. 161 |
| deadline · by | l'échéance · au plus tard le | SPF |
| first years self-employed | l'indépendant débutant | SPF |
| tax return | la déclaration | SPF |
| assessment (balance to pay or refunded) | l'avertissement-extrait de rôle | fin.belgium.be |
| advance ruling | la décision anticipée (Service des Décisions Anticipées, SDA) | circulaire 2021/C/20 |

## Director pay and personal tax

| English | French | Source |
|---|---|---|
| director · director pay | le dirigeant d'entreprise · la rémunération de dirigeant d'entreprise | art. 32, 51 |
| gross pay · taxable pay · net pay | la rémunération brute · imposable · nette | formule-clé n° 7 |
| net professional income (the pay's, before the marital quotient) | le revenu professionnel net | art. 130, 134 |
| income on the scale (each partner's own, plus or minus the quotient, plus other income) | le revenu au barème | art. 134 §4; art. 87-88 |
| paid after withholding (a month's amount, before contributions and the final tax) | versé après précompte (jamais « net payé ») | formule-clé |
| benefit in kind | l'avantage de toute nature (ATN) | art. 36 |
| flat-rate professional costs | les frais professionnels forfaitaires | formule-clé n° 8; art. 51 |
| income tax | l'impôt des personnes physiques (IPP) | art. 466 |
| progressive scale · bracket | le barème · la tranche | formule-clé; art. 130 |
| tax-free amount · its supplements | la quotité (du revenu) exemptée d'impôt · les suppléments | art. 134; avis d'indexation 2027 |
| dependent children · other dependants | les enfants à charge · les autres personnes à charge | avis d'indexation 2027 |
| single parent | isolé avec enfants à charge | avis d'indexation 2027 art. 133 |
| refundable tax credit for children | le crédit d'impôt remboursable pour enfants à charge | art. 134 §3 |
| tax credit for low earned income | le crédit d'impôt pour bas revenus d'activités | circulaire 2018; art. 289ter |
| marital quotient · income imputed to the partner (the partner's box on the map says only « imputé ») | le quotient conjugal · le revenu imputé au conjoint | avis d'indexation 2027 art. 87; formule-clé n° 11 |
| taxed jointly · taxed separately | l'imposition commune · imposé isolément | art. 134 |
| spouse or legal cohabitant · partner | le conjoint ou le cohabitant légal · le conjoint | avis d'indexation 2026 art. 126; formule-clé |
| State tax · autonomy factor | l'impôt État · le facteur d'autonomie | AR facteur d'autonomie 2017 |
| Walloon additional tax | les centimes additionnels régionaux | décret wallon 2017 art. 33 |
| municipal surcharge · municipal rate | la taxe communale additionnelle · le taux de la taxe communale additionnelle | art. 466, 468 |
| special social security contribution | la cotisation spéciale pour la sécurité sociale | circulaire CSSS |

## Withholding on pay

| English | French | Source |
|---|---|---|
| withholding on pay | le précompte professionnel (Pr.P) | formule-clé |
| key formula | la formule-clé | formule-clé |
| annual gross · annual net taxable · basic tax · annual tax | le revenu annuel brut · net imposable · l'impôt de base · l'impôt annuel | formule-clé n° 6–9 |
| reduction for dependent children | la réduction pour enfants à charge | formule-clé n° 13 |
| pay slip | la fiche de paie | not sourced |

## Dividends and movable income

| English | French | Source |
|---|---|---|
| dividend · withholding on it | le dividende · le précompte mobilier | art. 269 |
| exempt ordinary dividends | la première tranche (exonérée) des dividendes | art. 21 14° |
| VVPRbis shares · capital contribution | actions ou parts VVPRbis · l'apport (en numéraire) | art. 269 §2 |
| movable income | les revenus mobiliers | art. 37 |
| ordinary shares | les actions ordinaires | not sourced |

## Social contributions

| English | French | Source |
|---|---|---|
| social contributions · provisional · final | les cotisations sociales · provisoires · définitives | INASTI |
| social insurance fund · its management fee | la caisse d'assurances sociales · les frais de gestion | INASTI |
| self-employed in main occupation | l'indépendant à titre principal | INASTI |
| regularisation · minimum income | la régularisation · le revenu minimum | INASTI |
| reference year · revaluation coefficient | l'année de référence · le coefficient de réévaluation | INASTI dépliant 2026 |

## Company car

| English | French | Source |
|---|---|---|
| company car | la voiture de société (the law: le véhicule mis à disposition) | art. 36 |
| private use | l'utilisation à des fins personnelles | art. 36 |
| catalogue value · first registration | la valeur catalogue · la première immatriculation | art. 36 |
| CO2 percentage · reference emission | le pourcentage CO2 · l'émission de référence CO2 | art. 36 |
| your contribution | votre intervention (l'intervention du bénéficiaire) | art. 36 |
| the days you have it · you have it from | les jours de mise à disposition · à votre disposition depuis le | FAQ avantage véhicule, question 4 |
| you give it back on · given back | restituée le · la restitution | FAQ avantage véhicule, question 4 |
| prorated by calendar days | réduit prorata temporis sur base des jours calendrier | FAQ avantage véhicule, question 4 |
| plug-in hybrid · fake hybrid | l'hybride rechargeable · le faux hybride | art. 550; circulaire 2026/C/10 |
| fiscal horsepower | les chevaux fiscaux | art. 550 |
| petrol · diesel · LPG · natural gas · electric | essence · diesel · LPG · gaz naturel · électrique | art. 36 |

## Vouchers, allowances, rent, royalty

| English | French | Source |
|---|---|---|
| meal vouchers · eco vouchers · sport/culture vouchers | les titres-repas · les éco-chèques · les chèques sport/culture | art. 38/1 |
| company's share · face value | l'intervention de la société · la valeur nominale | art. 38/1 |
| days actually worked | les journées de travail effectivement fournies | art. 38/1 §2 |
| expense allowances | le remboursement de frais propres à la société | art. 32 al. 2 1° |
| office · internet · computer allowance | l'indemnité forfaitaire de bureau · d'internet · d'ordinateur | circulaire 2021/C/20 |
| structural and regular telework | le télétravail structurel et régulier | circulaire 2021/C/20 |
| cadastral income · revalued | le revenu cadastral (RC) · revalorisé | art. 13 |
| five thirds of the revalued RC | les cinq tiers du revenu cadastral revalorisé | art. 32 al. 2 3° |
| copyright royalty | les droits d'auteur (les revenus de droits d'auteur) | art. 17 §1 5° |
| taxed apart at 15 % | imposable distinctement au taux de 15 % | art. 171 |
| flat-rate costs (50 % / 25 %) | les frais forfaitaires | AR/CIR 92 art. 4 |
| creative share · envelope | le coefficient de créativité · l'enveloppe | décision anticipée 2026.0484 |
