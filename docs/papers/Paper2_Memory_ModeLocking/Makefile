PDF=paper.pdf
TEX=paper.tex
BIB=paper

.PHONY: pdf clean

pdf:
	pdflatex $(TEX)
	bibtex $(BIB)
	pdflatex $(TEX)
	pdflatex $(TEX)

clean:
	rm -f *.aux *.bbl *.blg *.log *.out *.toc
