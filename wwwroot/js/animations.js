// ISourceKO Dynamic Scrolling, Motion & Micro-Interaction Engine
// Built with GSAP, ScrollTrigger, and Lenis Smooth Scroll

(function() {
    let lenis = null;
    let marqueeTween = null;
    let magneticCleanups = [];

    function initSmoothScroll() {
        if (typeof Lenis === 'undefined') return;

        // Destroy existing instance if any
        if (lenis) {
            lenis.destroy();
        }

        lenis = new Lenis({
            duration: 1.1,
            easing: (t) => Math.min(1, 1.001 - Math.pow(2, -10 * t)),
            orientation: 'vertical',
            gestureOrientation: 'vertical',
            smoothWheel: true,
            wheelMultiplier: 0.9,
            touchMultiplier: 1.5,
        });

        if (typeof gsap !== 'undefined' && typeof ScrollTrigger !== 'undefined') {
            gsap.registerPlugin(ScrollTrigger);

            lenis.on('scroll', ScrollTrigger.update);

            gsap.ticker.add((time) => {
                lenis.raf(time * 1000);
            });

            gsap.ticker.lagSmoothing(0);
        }

        function raf(time) {
            lenis.raf(time);
            requestAnimationFrame(raf);
        }
        requestAnimationFrame(raf);
    }

    function initHeroEntrance() {
        if (typeof gsap === 'undefined') return;

        // 1. Staggered Masked Text Reveal (lines sliding up from overflow: hidden containers)
        const maskedLines = document.querySelectorAll('.masked-line-inner');
        if (maskedLines.length > 0) {
            gsap.fromTo(maskedLines, 
                { yPercent: 120, opacity: 0 },
                {
                    yPercent: 0,
                    opacity: 1,
                    duration: 1.05,
                    stagger: 0.12,
                    ease: "power3.out",
                    delay: 0.15
                }
            );
        }

        // Subtle scale-down effect on hero background media
        const heroBg = document.querySelector('.hero-media-scale');
        if (heroBg) {
            gsap.fromTo(heroBg,
                { scale: 1.08, opacity: 0.7 },
                {
                    scale: 1.0,
                    opacity: 1,
                    duration: 1.4,
                    ease: "power2.out"
                }
            );
        }
    }

    function initKineticMarquee() {
        if (typeof gsap === 'undefined' || typeof ScrollTrigger === 'undefined') return;

        const marqueeTrack = document.querySelector('.kinetic-marquee-track');
        if (!marqueeTrack) return;

        // Kill previous tween if any
        if (marqueeTween) marqueeTween.kill();

        // Continuous linear base scroll
        marqueeTween = gsap.to(marqueeTrack, {
            xPercent: -50,
            ease: "none",
            duration: 25,
            repeat: -1
        });

        // Kinetic response: speed and direction dynamically react to scroll velocity
        ScrollTrigger.create({
            trigger: document.body,
            start: "top top",
            end: "bottom bottom",
            onUpdate: (self) => {
                const velocity = self.getVelocity();
                if (Math.abs(velocity) > 50) {
                    // Normalize velocity multiplier
                    const speedMultiplier = 1 + Math.min(Math.abs(velocity) / 300, 5);
                    const direction = velocity > 0 ? 1 : -1;

                    gsap.to(marqueeTween, {
                        timeScale: speedMultiplier * direction,
                        duration: 0.4,
                        overwrite: true
                    });

                    // Decay back to base speed of 1
                    gsap.to(marqueeTween, {
                        timeScale: 1,
                        duration: 1.2,
                        delay: 0.4,
                        ease: "power2.out",
                        overwrite: false
                    });
                }
            }
        });
    }

    function initSectionTransitions() {
        if (typeof gsap === 'undefined' || typeof ScrollTrigger === 'undefined') return;

        // 4. Seamless Section Transitions: Subtle fade-and-rise (y: 40 to 0, opacity: 0 to 1) with power3.out
        const revealElements = document.querySelectorAll('.reveal-on-scroll');
        revealElements.forEach((el) => {
            gsap.fromTo(el,
                { y: 40, opacity: 0 },
                {
                    y: 0,
                    opacity: 1,
                    duration: 0.9,
                    ease: "power3.out",
                    scrollTrigger: {
                        trigger: el,
                        start: "top 88%",
                        toggleActions: "play none none none"
                    }
                }
            );
        });

        // 2. Scroll-Linked Sticky Split-Screen Section (Desktop only for 60fps responsiveness)
        const splitSection = document.querySelector('.sticky-split-container');
        if (splitSection && window.innerWidth >= 1024) {
            const visualPanels = splitSection.querySelectorAll('.split-visual-panel');
            const storyCards = splitSection.querySelectorAll('.split-story-card');

            storyCards.forEach((card, index) => {
                ScrollTrigger.create({
                    trigger: card,
                    start: "top 60%",
                    end: "bottom 60%",
                    onEnter: () => activatePanel(index),
                    onEnterBack: () => activatePanel(index)
                });
            });

            function activatePanel(targetIndex) {
                visualPanels.forEach((panel, i) => {
                    if (i === targetIndex) {
                        gsap.to(panel, { opacity: 1, scale: 1, duration: 0.45, ease: "power2.out", overwrite: true });
                    } else {
                        gsap.to(panel, { opacity: 0, scale: 0.96, duration: 0.35, ease: "power2.out", overwrite: true });
                    }
                });
            }
        }
    }

    function initMagneticButtons() {
        if (typeof gsap === 'undefined') return;

        // Clean up previous event listeners
        magneticCleanups.forEach(fn => fn());
        magneticCleanups = [];

        // 5. Magnetic Hover Effects on Primary Buttons
        const magneticBtns = document.querySelectorAll('.magnetic-btn');
        magneticBtns.forEach((btn) => {
            const handleMouseMove = (e) => {
                const rect = btn.getBoundingClientRect();
                const x = (e.clientX - rect.left - rect.width / 2) * 0.3;
                const y = (e.clientY - rect.top - rect.height / 2) * 0.3;

                gsap.to(btn, {
                    x: x,
                    y: y,
                    duration: 0.25,
                    ease: "power2.out"
                });
            };

            const handleMouseLeave = () => {
                gsap.to(btn, {
                    x: 0,
                    y: 0,
                    duration: 0.6,
                    ease: "elastic.out(1, 0.4)"
                });
            };

            btn.addEventListener('mousemove', handleMouseMove);
            btn.addEventListener('mouseleave', handleMouseLeave);

            magneticCleanups.push(() => {
                btn.removeEventListener('mousemove', handleMouseMove);
                btn.removeEventListener('mouseleave', handleMouseLeave);
            });
        });
    }

    // Master initialization function
    window.initISourceKOAnimations = function() {
        initSmoothScroll();
        initHeroEntrance();
        initKineticMarquee();
        initSectionTransitions();
        initMagneticButtons();
    };

    // Auto initialize on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', window.initISourceKOAnimations);
    } else {
        setTimeout(window.initISourceKOAnimations, 50);
    }
})();
