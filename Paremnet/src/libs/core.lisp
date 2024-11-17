(package-set "core")

(package-export 
	'(let let* letrec define 
	  and or cond case
	  for dotimes

	  first second third rest
	  after-first after-second after-third
	  fold-left fold-right reverse
	  chain chain-list
	  ))
	

;;
;;
;; MACROS

;; (let ((x 1) (y 2)) 
;;    (+ x 1)) 
;; => 
;; ((lambda (x y) (+ x y)) 1 2)
;;
;; e.g. (let ((x 1)) (+ x 1)) => 2
;;
(defmacro let (bindings . body) 
	`((lambda ,(map car bindings) ,@body) 
		,@(map cadr bindings)))

;; (let* ((x 1) (y 2)) 
;;    (+ x y)) 
;; => 
;; (let ((x 1)) (let ((y 2)) (+ x y)))
;;
(defmacro let* (bindings . body)
	(if (null? bindings)
		`(begin ,@body)
		`(let (,(car bindings))
			(let* ,(cdr bindings) ,@body))))
		
;; (letrec ((x (lambda () y)) 
;;          (y 1)) 
;;   (x)) 
;; => 
;; (let ((x nil) (y nil)) 
;;   (set! x (lambda () y)) 
;;   (set! y 1) 
;;   (x))
;;
(defmacro letrec (bindings . body)
	`(let ,(map (lambda (v) (list (car v) nil)) bindings)
		,@(map (lambda (v) `(set! ,@v)) bindings)
		,@body))

;; (define foo 5) => (begin (set! foo 5) 'foo)
;; (define (foo x) 5) => (define foo (lambda (x) 5))
;;
(defmacro define (name . body)
	(if (atom? name)
		`(begin (set! ,name ,@body) (quote ,name))
		`(define ,(car name) 
			(lambda ,(cdr name) ,@body))))

;; (and x) => x
;; (and x y) => (if x y #f)
;; (and x y z) => (if x (and y z) #f) => (if x (if y z) #f)
;;
(defmacro and (first . rest)
	(if (null? rest) 
		(car (list first))
		(if (= (length rest) 1)
			`(if ,first ,@rest #f)
			`(if ,first (and ,@rest) #f))))

;; (or x) => x
;; (or x y) => (if* x y)
;; (or x y z) => (if* x (or y z)) => (if* x (if* y z))
;;
(defmacro or (first . rest) 
	(if (null? rest) 
		(car (list first))
		(if (= (length rest) 1)
			`(if* ,first ,@rest)
			`(if* ,first (or ,@rest)))))
			
;; (cond ((= a b) 1) 
;;		 ((= a c) 2) 
;;		 3)
;; => 
;; (if (= a b) (begin 1) (if (= a c) (begin 2) (begin 3)))
;;
(defmacro cond (first . rest)
	(if (null? rest)
		`(begin ,first)
		`(if ,(car first) 
			(begin ,@(cdr first))
			(cond ,@rest))))
			
;; (case (+ 1 2) 
;;		 (3 "foo") 
;;		 (4 "bar") 
;;		 "baz")
;; =>
;; (let (GENSYM-xxx (+ 1 2)) 
;;  	(cond ((= GENSYM-xxx 3) "foo")
;;			  ((= GENSYM-xxx 4) "bar")
;;			  "baz"))
;;
(defmacro case (key . rest)
	(let* ((keyval (gensym "CASE")))
		`(let ((,keyval ,key))
			(cond
			   ,@(map (lambda (elt) 
								(if (cons? elt) 
									(cons (list '= keyval (car elt)) (cdr elt))
									elt))
						rest)))))
						
;; (for (x 0 (< x 10) (+ x 1)) (trace x) ...)
;; =>
;; (let ((x 0)))
;;     (while (< x 10) 
;;         (trace x) ... 
;;         (set! x (+ x 1))))
(defmacro for (test . body)
    (let ((varname (car test)) 
	      (init-value (cadr test)) 
		  (predicate (caddr test)) 
		  (step-value (car (cdddr test))))
	    `(let ((,varname ,init-value)) 
		    (while ,predicate ,@body (set! ,varname ,step-value)))))

;; (dotimes (x 10) (trace x) ...)
;; =>
;; (for (x 0 (< x 10) (+ x 1)) (trace x) ...)
(defmacro dotimes (pars . body) 
    (let ((varname (car pars))
	      (count (cadr pars)))
        `(for (,varname 0 (< ,varname ,count) (+ ,varname 1))
		    ,@body)))

;; (apply + '(1 2))
;; =>
;; (+ 1 2)
(defmacro apply (fn args) 
    (let ((arglist (eval args))) 
	    `(,fn ,@arglist)))
		   

												
;; 
;;
;; FUNCTIONS

(define first car)
(define second cadr)
(define third caddr)
(define rest cdr)
(define after-first cdr)
(define after-second cddr)
(define after-third cdddr)

;; (fold-left cons '() '(1 2 3)) 
;; 			=> (((() . 1) . 2) . 3) 
;; because 	=> '(cons (cons (cons '() 1) 2) 3) 
;;
(define (fold-left fn base lst)
	(if (= (length lst) 0) 
		base
		(fold-left fn (fn base (car lst)) (cdr lst))))

;; (fold-right cons '() '(1 2 3)) 
;;			=> '(1 2 3) 
;; because 	=> '(cons 1 (cons 2 (cons 3 '())))
;;
(define (fold-right fn base lst)
	(if (= (length lst) 0) 
		base
		(fn (car lst) (fold-right fn base (cdr lst)))))

;; (zip '(1 2) '(a b)) 
;;   => '((1 a) (2 b))
(define (zip a b)
	(if (or (null? a) (null? b))
		'()
		(cons (list (car a) (car b)) (zip (cdr a) (cdr b)))))

;; (reverse '(1 2 3)) 
;;   => '(3 2 1)
(define (reverse lst)
  	(define (helper lst result)
    	(if (null? lst)
      		result
      		(helper (cdr lst) (cons (car lst) result))))
  	(helper lst '()))


;; (index-of 'b '(a b c)) => 1
;; (index-of 'b '(1 2 3)) => ()
(define (index-of elt lst)
	(letrec ((helper (lambda (l e i) (cond 
						((null? l) l)
						((= e (first l)) i)
						(helper (rest l) e (+ i 1))))))
		(helper lst elt 0)))	



;;
;; MORE MACROS THAT DEPEND ON THE ABOVE

;; (chain-list (list first second third))
;; =>
;; (lambda (GENSYM-123) (third (second (first GENSYM-123))))
;; 
;; e.g. (chain-list (list cdr cdr car)) == caddr
;;
(defmacro chain-list (lst)
    (let* ((var (gensym))
	       (args (reverse (eval lst)))
	       (bodytext (eval `(fold-right list ',var ',args))))
	  `(lambda (,var) ,bodytext)))

;; (chain first second third)
;; =>
;; (lambda (GENSYM-123) (third (second (first GENSYM-123))))
;;
;; e.g. (chain cdr cdr car) => caddr
;;
(defmacro chain (first . rest)
    `(chain-list ',(cons first rest)))



;;
;;
;; TODO INTEROP

;; (.. struct '(field1 field2)) 
;;		=> (deref (deref struct field1) field2)
;;
;;(define (.. obj params)
;;	(fold-left deref obj params))
